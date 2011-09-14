/*
CUDA BarnesHut v2.0: Simulation of the gravitational forces
in a galactic cluster using the Barnes-Hut n-body algorithm

Copyright (c) 2011, Texas State University-San Marcos.  All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

   * Redistributions of source code must retain the above copyright notice, 
     this list of conditions and the following disclaimer.
   * Redistributions in binary form must reproduce the above copyright notice,
     this list of conditions and the following disclaimer in the documentation
     and/or other materials provided with the distribution.
   * Neither the name of Texas State University-San Marcos nor the names of its
     contributors may be used to endorse or promote products derived from this
     software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED
IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE
OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
OF THE POSSIBILITY OF SUCH DAMAGE.

Author: Martin Burtscher
*/


#include <stdlib.h>
#include <stdio.h>
#include <math.h>
#include <time.h>
#include <cuda.h>


// thread count
#define THREADS1 512  /* must be a power of 2 */
#define THREADS2 1024
#define THREADS3 1024
#define THREADS4 256
#define THREADS5 256
#define THREADS6 512

// block count = factor * #SMs
#define FACTOR1 3
#define FACTOR2 1
#define FACTOR3 1  /* must all be resident at the same time */
#define FACTOR4 1  /* must all be resident at the same time */
#define FACTOR5 5
#define FACTOR6 3

#define WARPSIZE 32
#define MAXDEPTH 32


/******************************************************************************/

// childd is aliased with velxd, velyd, velzd, accxd, accyd, acczd, and sortd but they never use the same memory locations
__constant__ int nnodesd, nbodiesd;
__constant__ float dtimed, dthfd, epssqd, itolsqd;
__constant__ volatile float *massd, *posxd, *posyd, *poszd, *velxd, *velyd, *velzd, *accxd, *accyd, *acczd;
__constant__ volatile float *maxxd, *maxyd, *maxzd, *minxd, *minyd, *minzd;
__constant__ volatile int *errd, *sortd, *childd, *countd, *startd;

__device__ volatile int stepd, bottomd, maxdepthd, blkcntd;
__device__ volatile float radiusd;


/******************************************************************************/
/*** initialize memory ********************************************************/
/******************************************************************************/

__global__ void InitializationKernel()
{
  *errd = 0;
  stepd = -1;
  maxdepthd = 1;
  blkcntd = 0;
}


/******************************************************************************/
/*** compute center and radius ************************************************/
/******************************************************************************/

__global__
__launch_bounds__(THREADS1, FACTOR1)
void BoundingBoxKernel()
{
  register int i, j, k, inc;
  register float val, minx, maxx, miny, maxy, minz, maxz;
  __shared__ volatile float sminx[THREADS1], smaxx[THREADS1], sminy[THREADS1], smaxy[THREADS1], sminz[THREADS1], smaxz[THREADS1];

  // initialize with valid data (in case #bodies < #threads)
  minx = maxx = posxd[0];
  miny = maxy = posyd[0];
  minz = maxz = poszd[0];

  // scan all bodies
  i = threadIdx.x;
  inc = THREADS1 * gridDim.x;
  for (j = i + blockIdx.x * THREADS1; j < nbodiesd; j += inc) {
    val = posxd[j];
    minx = min(minx, val);
    maxx = max(maxx, val);
    val = posyd[j];
    miny = min(miny, val);
    maxy = max(maxy, val);
    val = poszd[j];
    minz = min(minz, val);
    maxz = max(maxz, val);
  }

  // reduction in shared memory
  sminx[i] = minx;
  smaxx[i] = maxx;
  sminy[i] = miny;
  smaxy[i] = maxy;
  sminz[i] = minz;
  smaxz[i] = maxz;

  for (j = THREADS1 / 2; j > 0; j /= 2) {
    __syncthreads();
    if (i < j) {
      k = i + j;
      sminx[i] = minx = min(minx, sminx[k]);
      smaxx[i] = maxx = max(maxx, smaxx[k]);
      sminy[i] = miny = min(miny, sminy[k]);
      smaxy[i] = maxy = max(maxy, smaxy[k]);
      sminz[i] = minz = min(minz, sminz[k]);
      smaxz[i] = maxz = max(maxz, smaxz[k]);
    }
  }

  // write block result to global memory
  if (i == 0) {
    k = blockIdx.x;
    minxd[k] = minx;
    maxxd[k] = maxx;
    minyd[k] = miny;
    maxyd[k] = maxy;
    minzd[k] = minz;
    maxzd[k] = maxz;
    __threadfence();

    inc = gridDim.x - 1;
    if (inc == atomicInc((unsigned int *)&blkcntd, inc)) {
      // I'm the last block, so combine all block results
      for (j = 0; j <= inc; j++) {
        minx = min(minx, minxd[j]);
        maxx = max(maxx, maxxd[j]);
        miny = min(miny, minyd[j]);
        maxy = max(maxy, maxyd[j]);
        minz = min(minz, minzd[j]);
        maxz = max(maxz, maxzd[j]);
      }

      // compute 'radius'
      val = max(maxx - minx, maxy - miny);
      radiusd = max(val, maxz - minz) * 0.5f;

      // create root node
      k = nnodesd;
      bottomd = k;

      massd[k] = -1.0f;
      startd[k] = 0;
      posxd[k] = (minx + maxx) * 0.5f;
      posyd[k] = (miny + maxy) * 0.5f;
      poszd[k] = (minz + maxz) * 0.5f;
      k *= 8;
      for (i = 0; i < 8; i++) childd[k + i] = -1;

      stepd++;
    }
  }
}


/******************************************************************************/
/*** build tree ***************************************************************/
/******************************************************************************/

__global__
__launch_bounds__(THREADS2, FACTOR2)
void TreeBuildingKernel()
{
  register int i, j, k, depth, localmaxdepth, skip, inc;
  register float x, y, z, r;
  register float px, py, pz;
  register int ch, n, cell, locked, patch;
  register float radius, rootx, rooty, rootz;

  // cache root data
  radius = radiusd;
  rootx = posxd[nnodesd];
  rooty = posyd[nnodesd];
  rootz = poszd[nnodesd];

  localmaxdepth = 1;
  skip = 1;
  inc = blockDim.x * gridDim.x;
  i = threadIdx.x + blockIdx.x * blockDim.x;

  // iterate over all bodies assigned to thread
  while (i < nbodiesd) {
    if (skip != 0) {
      // new body, so start traversing at root
      skip = 0;
      px = posxd[i];
      py = posyd[i];
      pz = poszd[i];
      n = nnodesd;
      depth = 1;
      r = radius;
      j = 0;
      // determine which child to follow
      if (rootx < px) j = 1;
      if (rooty < py) j += 2;
      if (rootz < pz) j += 4;
    }

    // follow path to leaf cell
    ch = childd[n*8+j];
    while (ch >= nbodiesd) {
      n = ch;
      depth++;
      r *= 0.5f;
      j = 0;
      // determine which child to follow
      if (posxd[n] < px) j = 1;
      if (posyd[n] < py) j += 2;
      if (poszd[n] < pz) j += 4;
      ch = childd[n*8+j];
    }

    if (ch != -2) {  // skip if child pointer is locked and try again later
      locked = n*8+j;
      if (ch == atomicCAS((int *)&childd[locked], ch, -2)) {  // try to lock
        if (ch == -1) {
          // if null, just insert the new body
          childd[locked] = i;
        } else {  // there already is a body in this position
          patch = -1;
          // create new cell(s) and insert the old and new body
          do {
            depth++;

            cell = atomicSub((int *)&bottomd, 1) - 1;
            if (cell <= nbodiesd) {
              *errd = 1;
              bottomd = nnodesd;
            }
            patch = max(patch, cell);

            x = (j & 1) * r;
            y = ((j >> 1) & 1) * r;
            z = ((j >> 2) & 1) * r;
            r *= 0.5f;

            massd[cell] = -1.0f;
            startd[cell] = -1;
            x = posxd[cell] = posxd[n] - r + x;
            y = posyd[cell] = posyd[n] - r + y;
            z = poszd[cell] = poszd[n] - r + z;
            for (k = 0; k < 8; k++) childd[cell*8+k] = -1;

            if (patch != cell) { 
              childd[n*8+j] = cell;
            }

            j = 0;
            if (x < posxd[ch]) j = 1;
            if (y < posyd[ch]) j += 2;
            if (z < poszd[ch]) j += 4;
            childd[cell*8+j] = ch;

            n = cell;
            j = 0;
            if (x < px) j = 1;
            if (y < py) j += 2;
            if (z < pz) j += 4;

            ch = childd[n*8+j];
            // repeat until the two bodies are different children
          } while (ch >= 0);
          childd[n*8+j] = i;
          __threadfence();  // push out subtree
          childd[locked] = patch;
        }
        __threadfence();  // push out results

        localmaxdepth = max(depth, localmaxdepth);
        i += inc;  // move on to next body
        skip = 1;
      }
    }
    __syncthreads();  // throttle
  }
  // record maximum tree depth
  atomicMax((int *)&maxdepthd, localmaxdepth);
}


/******************************************************************************/
/*** compute center of mass ***************************************************/
/******************************************************************************/

__global__
__launch_bounds__(THREADS3, FACTOR3)
void SummarizationKernel()
{
  register int i, j, k, ch, inc, missing, cnt, bottom;
  register float m, cm, px, py, pz;
  __shared__ volatile int child[THREADS3 * 8];

  bottom = bottomd;
  inc = blockDim.x * gridDim.x;
  k = (bottom & (-WARPSIZE)) + threadIdx.x + blockIdx.x * blockDim.x;  // align to warp size
  if (k < bottom) k += inc;

  missing = 0;
  // iterate over all cells assigned to thread
  while (k <= nnodesd) {
    if (missing == 0) {
      // new cell, so initialize
      cm = 0.0f;
      px = 0.0f;
      py = 0.0f;
      pz = 0.0f;
      cnt = 0;
      j = 0;
      for (i = 0; i < 8; i++) {
        ch = childd[k*8+i];
        if (ch >= 0) {
          if (i != j) {
            // move children to front (needed later for speed)
            childd[k*8+i] = -1;
            childd[k*8+j] = ch;
          }
          child[missing*THREADS3+threadIdx.x] = ch;  // cache missing children
          m = massd[ch];
          missing++;
          if (m >= 0.0f) {
            // child is ready
            missing--;
            if (ch >= nbodiesd) {  // count bodies (needed later)
              cnt += countd[ch] - 1;
            }
            // add child's contribution
            cm += m;
            px += posxd[ch] * m;
            py += posyd[ch] * m;
            pz += poszd[ch] * m;
          }
          j++;
        }
      }
      __threadfence();  // for performance only
      cnt += j;
    }

    if (missing != 0) {
      do {
        // poll missing child
        ch = child[(missing-1)*THREADS3+threadIdx.x];
        m = massd[ch];
        if (m >= 0.0f) {
          // child is now ready
          missing--;
          if (ch >= nbodiesd) {
            // count bodies (needed later)
            cnt += countd[ch] - 1;
          }
          // add child's contribution
          cm += m;
          px += posxd[ch] * m;
          py += posyd[ch] * m;
          pz += poszd[ch] * m;
        }
        // repeat until we are done or child is not ready
      } while ((m >= 0.0f) && (missing != 0));
    }

    if (missing == 0) {
      // all children are ready, so store computed information
      countd[k] = cnt;
      m = 1.0f / cm;
      posxd[k] = px * m;
      posyd[k] = py * m;
      poszd[k] = pz * m;
      __threadfence();  // make sure data are visible before setting mass
      massd[k] = cm;
      __threadfence();  // push out results
      k += inc;  // move on to next cell
    }
  }
}


/******************************************************************************/
/*** sort bodies **************************************************************/
/******************************************************************************/

__global__
__launch_bounds__(THREADS4, FACTOR4)
void SortKernel()
{
  register int i, k, ch, dec, start, bottom;

  bottom = bottomd;
  dec = blockDim.x * gridDim.x;
  k = nnodesd + 1 - dec + threadIdx.x + blockIdx.x * blockDim.x;

  // iterate over all cells assigned to thread
  while (k >= bottom) {
    start = startd[k];
    if (start >= 0) {
      for (i = 0; i < 8; i++) {
        ch = childd[k*8+i];
        if (ch >= nbodiesd) {
          // child is a cell
          startd[ch] = start;  // set start ID of child
          start += countd[ch];  // add #bodies in subtree
        } else if (ch >= 0) {
          // child is a body
          sortd[start] = ch;  // record body in 'sorted' array
          start++;
        }
      }
      k -= dec;  // move on to next cell
    }
    __syncthreads();  // throttle
  }
}


/******************************************************************************/
/*** compute force ************************************************************/
/******************************************************************************/

__global__
__launch_bounds__(THREADS5, FACTOR5)
void ForceCalculationKernel()
{
  register int i, j, k, n, depth, base, sbase, diff;
  register float px, py, pz, ax, ay, az, dx, dy, dz, tmp;
  __shared__ volatile int pos[MAXDEPTH * THREADS5/WARPSIZE], node[MAXDEPTH * THREADS5/WARPSIZE];
  __shared__ volatile float dq[MAXDEPTH * THREADS5/WARPSIZE];
  __shared__ volatile int step, maxdepth;

  if (0 == threadIdx.x) {
    step = stepd;
    maxdepth = maxdepthd;
    tmp = radiusd;
    // precompute values that depend only on tree level
    dq[0] = tmp * tmp * itolsqd;
    for (i = 1; i < maxdepth; i++) {
      dq[i] = dq[i - 1] * 0.25f;
    }

    if (maxdepth > MAXDEPTH) {
      *errd = maxdepth;
    }
  }
  __syncthreads();

  if (maxdepth <= MAXDEPTH) {
    // figure out first thread in each warp (lane 0)
    base = threadIdx.x / WARPSIZE;
    sbase = base * WARPSIZE;
    j = base * MAXDEPTH;

    diff = threadIdx.x - sbase;
    // make multiple copies to avoid index calculations later
    if (diff < MAXDEPTH) {
      dq[diff+j] = dq[diff];
    }
    __syncthreads();

    // iterate over all bodies assigned to thread
    for (k = threadIdx.x + blockIdx.x * blockDim.x; k < nbodiesd; k += blockDim.x * gridDim.x) {
      i = sortd[k];  // get permuted/sorted index
      // cache position info
      px = posxd[i];
      py = posyd[i];
      pz = poszd[i];

      ax = 0.0f;
      ay = 0.0f;
      az = 0.0f;

      // initialize iteration stack, i.e., push root node onto stack
      depth = j;
      if (sbase == threadIdx.x) {
        node[j] = nnodesd;
        pos[j] = 0;
      }
      __threadfence();  // make sure it's visible

      while (depth >= j) {
        // stack is not empty
        while (pos[depth] < 8) {
          // node on top of stack has more children to process
          n = childd[node[depth]*8+pos[depth]];  // load child pointer
          if (sbase == threadIdx.x) {
            // I'm the first thread in the warp
            pos[depth]++;
          }
          __threadfence();  // make sure it's visible
          if (n >= 0) {
            dx = posxd[n] - px;
            dy = posyd[n] - py;
            dz = poszd[n] - pz;
            tmp = dx*dx + (dy*dy + (dz*dz + epssqd));  // compute distance squared (plus softening)
            if ((n < nbodiesd) || __all(tmp >= dq[depth])) {  // check if all threads agree that cell is far enough away (or is a body)
              tmp = rsqrtf(tmp);  // compute distance
              tmp = massd[n] * tmp * tmp * tmp;
              ax += dx * tmp;
              ay += dy * tmp;
              az += dz * tmp;
            } else {
              // push cell onto stack
              depth++;
              if (sbase == threadIdx.x) {
                node[depth] = n;
                pos[depth] = 0;
              }
              __threadfence();  // make sure it's visible
            }
          } else {
            depth = max(j, depth - 1);  // early out because all remaining children are also zero
          }
        }
        depth--;  // done with this level
      }

      if (step > 0) {
        // update velocity
        velxd[i] += (ax - accxd[i]) * dthfd;
        velyd[i] += (ay - accyd[i]) * dthfd;
        velzd[i] += (az - acczd[i]) * dthfd;
      }

      // save computed acceleration
      accxd[i] = ax;
      accyd[i] = ay;
      acczd[i] = az;
    }
  }
}


/******************************************************************************/
/*** advance bodies ***********************************************************/
/******************************************************************************/

__global__
__launch_bounds__(THREADS6, FACTOR6)
void IntegrationKernel()
{
  register int i, inc;
  register float dvelx, dvely, dvelz;
  register float velhx, velhy, velhz;

  // iterate over all bodies assigned to thread
  inc = blockDim.x * gridDim.x;
  for (i = threadIdx.x + blockIdx.x * blockDim.x; i < nbodiesd; i += inc) {
    // integrate
    dvelx = accxd[i] * dthfd;
    dvely = accyd[i] * dthfd;
    dvelz = acczd[i] * dthfd;

    velhx = velxd[i] + dvelx;
    velhy = velyd[i] + dvely;
    velhz = velzd[i] + dvelz;

    posxd[i] += velhx * dtimed;
    posyd[i] += velhy * dtimed;
    poszd[i] += velhz * dtimed;

    velxd[i] = velhx + dvelx;
    velyd[i] = velhy + dvely;
    velzd[i] = velhz + dvelz;
  }
}


/******************************************************************************/

static void CudaTest(char *msg)
{
  cudaError_t e;

  cudaThreadSynchronize();
  if (cudaSuccess != (e = cudaGetLastError())) {
    fprintf(stderr, "%s: %d\n", msg, e);
    fprintf(stderr, "%s\n", cudaGetErrorString(e));
    exit(-1);
  }
}


/******************************************************************************/

// random number generator

#define MULT 1103515245
#define ADD 12345
#define MASK 0x7FFFFFFF
#define TWOTO31 2147483648.0

static int A = 1;
static int B = 0;
static int randx = 1;
static int lastrand;


static void drndset(int seed)
{
   A = 1;
   B = 0;
   randx = (A * seed + B) & MASK;
   A = (MULT * A) & MASK;
   B = (MULT * B + ADD) & MASK;
}


static double drnd()
{
   lastrand = randx;
   randx = (A * randx + B) & MASK;
   return (double)lastrand / TWOTO31;
}


/******************************************************************************/

int main(int argc, char *argv[])
{
  register int i, run, blocks;
  register int nnodes, nbodies, step, timesteps;
  register int runtime, mintime;
  int error;
  register float dtime, dthf, epssq, itolsq;
  float time, timing[7];
  clock_t starttime, endtime;
  cudaEvent_t start, stop;
  float *mass, *posx, *posy, *posz, *velx, *vely, *velz;

  int *errl, *sortl, *childl, *countl, *startl;
  float *massl;
  float *posxl, *posyl, *poszl;
  float *velxl, *velyl, *velzl;
  float *accxl, *accyl, *acczl;
  float *maxxl, *maxyl, *maxzl;
  float *minxl, *minyl, *minzl;
  register double rsc, vsc, r, v, x, y, z, sq, scale;

  // perform some checks

  fprintf(stderr, "CUDA BarnesHut v2.0\n");
  if (argc != 3) {
    fprintf(stderr, "\n");
    fprintf(stderr, "arguments: number_of_bodies number_of_timesteps\n");
    exit(-1);
  }

  int deviceCount;
  cudaGetDeviceCount(&deviceCount);
  if (deviceCount == 0) {
    fprintf(stderr, "There is no device supporting CUDA\n");
    exit(-1);
  }
  cudaDeviceProp deviceProp;
  cudaGetDeviceProperties(&deviceProp, 0);
  if ((deviceProp.major == 9999) && (deviceProp.minor == 9999)) {
    fprintf(stderr, "There is no CUDA capable device\n");
    exit(-1);
  }
  if (deviceProp.major < 2) {
    fprintf(stderr, "Need at least compute capability 2.0\n");
    exit(-1);
  }
  if (deviceProp.warpSize != WARPSIZE) {
    fprintf(stderr, "Warp size must be %d\n", deviceProp.warpSize);
    exit(-1);
  }

  blocks = deviceProp.multiProcessorCount;
  fprintf(stderr, "blocks = %d\n", blocks);

  if ((WARPSIZE <= 0) || (WARPSIZE & (WARPSIZE-1) != 0)) {
    fprintf(stderr, "Warp size must be greater than zero and a power of two\n");
    exit(-1);
  }
  if (MAXDEPTH > WARPSIZE) {
    fprintf(stderr, "MAXDEPTH must be less than or equal to WARPSIZE\n");
    exit(-1);
  }
  if ((THREADS1 <= 0) || (THREADS1 & (THREADS1-1) != 0)) {
    fprintf(stderr, "THREADS1 must be greater than zero and a power of two\n");
    exit(-1);
  }

  // set L1/shared memory configuration
  cudaFuncSetCacheConfig(BoundingBoxKernel, cudaFuncCachePreferShared);
  cudaFuncSetCacheConfig(TreeBuildingKernel, cudaFuncCachePreferL1);
  cudaFuncSetCacheConfig(SummarizationKernel, cudaFuncCachePreferShared);
  cudaFuncSetCacheConfig(SortKernel, cudaFuncCachePreferL1);
  cudaFuncSetCacheConfig(ForceCalculationKernel, cudaFuncCachePreferL1);
  cudaFuncSetCacheConfig(IntegrationKernel, cudaFuncCachePreferL1);

  cudaGetLastError();  // reset error value
  for (run = 0; run < 3; run++) {
    for (i = 0; i < 7; i++) timing[i] = 0.0f;

    nbodies = atoi(argv[1]);
    if (nbodies < 1) {
      fprintf(stderr, "nbodies is too small: %d\n", nbodies);
      exit(-1);
    }
    if (nbodies > (1 << 30)) {
      fprintf(stderr, "nbodies is too large: %d\n", nbodies);
      exit(-1);
    }
    nnodes = nbodies * 2;
    if (nnodes < 1024*blocks) nnodes = 1024*blocks;
    while ((nnodes & (WARPSIZE-1)) != 0) nnodes++;
    nnodes--;

    timesteps = atoi(argv[2]);
    dtime = 0.025;  dthf = dtime * 0.5f;
    epssq = 0.05 * 0.05;
    itolsq = 1.0f / (0.5 * 0.5);

    // allocate memory

    if (run == 0) {
      fprintf(stderr, "nodes = %d\n", nnodes+1);
      fprintf(stderr, "configuration: %d bodies, %d time steps\n", nbodies, timesteps);

      mass = (float *)malloc(sizeof(float) * nbodies);
      if (mass == NULL) {fprintf(stderr, "cannot allocate mass\n");  exit(-1);}
      posx = (float *)malloc(sizeof(float) * nbodies);
      if (posx == NULL) {fprintf(stderr, "cannot allocate posx\n");  exit(-1);}
      posy = (float *)malloc(sizeof(float) * nbodies);
      if (posy == NULL) {fprintf(stderr, "cannot allocate posy\n");  exit(-1);}
      posz = (float *)malloc(sizeof(float) * nbodies);
      if (posz == NULL) {fprintf(stderr, "cannot allocate posz\n");  exit(-1);}
      velx = (float *)malloc(sizeof(float) * nbodies);
      if (velx == NULL) {fprintf(stderr, "cannot allocate velx\n");  exit(-1);}
      vely = (float *)malloc(sizeof(float) * nbodies);
      if (vely == NULL) {fprintf(stderr, "cannot allocate vely\n");  exit(-1);}
      velz = (float *)malloc(sizeof(float) * nbodies);
      if (velz == NULL) {fprintf(stderr, "cannot allocate velz\n");  exit(-1);}

      if (cudaSuccess != cudaMalloc((void **)&errl, sizeof(int))) fprintf(stderr, "could not allocate errd\n");  CudaTest("couldn't allocate errd");
      if (cudaSuccess != cudaMalloc((void **)&childl, sizeof(int) * (nnodes+1) * 8)) fprintf(stderr, "could not allocate childd\n");  CudaTest("couldn't allocate childd");
      if (cudaSuccess != cudaMalloc((void **)&massl, sizeof(float) * (nnodes+1))) fprintf(stderr, "could not allocate massd\n");  CudaTest("couldn't allocate massd");
      if (cudaSuccess != cudaMalloc((void **)&posxl, sizeof(float) * (nnodes+1))) fprintf(stderr, "could not allocate posxd\n");  CudaTest("couldn't allocate posxd");
      if (cudaSuccess != cudaMalloc((void **)&posyl, sizeof(float) * (nnodes+1))) fprintf(stderr, "could not allocate posyd\n");  CudaTest("couldn't allocate posyd");
      if (cudaSuccess != cudaMalloc((void **)&poszl, sizeof(float) * (nnodes+1))) fprintf(stderr, "could not allocate poszd\n");  CudaTest("couldn't allocate poszd");
      if (cudaSuccess != cudaMalloc((void **)&countl, sizeof(int) * (nnodes+1))) fprintf(stderr, "could not allocate countd\n");  CudaTest("couldn't allocate countd");
      if (cudaSuccess != cudaMalloc((void **)&startl, sizeof(int) * (nnodes+1))) fprintf(stderr, "could not allocate startd\n");  CudaTest("couldn't allocate startd");

      // alias arrays
      int inc = (nbodies + WARPSIZE - 1) & (-WARPSIZE);
      velxl = (float *)&childl[0*inc];
      velyl = (float *)&childl[1*inc];
      velzl = (float *)&childl[2*inc];
      accxl = (float *)&childl[3*inc];
      accyl = (float *)&childl[4*inc];
      acczl = (float *)&childl[5*inc];
      sortl = (int *)&childl[6*inc];

      if (cudaSuccess != cudaMalloc((void **)&maxxl, sizeof(float) * blocks)) fprintf(stderr, "could not allocate maxxd\n");  CudaTest("couldn't allocate maxxd");
      if (cudaSuccess != cudaMalloc((void **)&maxyl, sizeof(float) * blocks)) fprintf(stderr, "could not allocate maxyd\n");  CudaTest("couldn't allocate maxyd");
      if (cudaSuccess != cudaMalloc((void **)&maxzl, sizeof(float) * blocks)) fprintf(stderr, "could not allocate maxzd\n");  CudaTest("couldn't allocate maxzd");
      if (cudaSuccess != cudaMalloc((void **)&minxl, sizeof(float) * blocks)) fprintf(stderr, "could not allocate minxd\n");  CudaTest("couldn't allocate minxd");
      if (cudaSuccess != cudaMalloc((void **)&minyl, sizeof(float) * blocks)) fprintf(stderr, "could not allocate minyd\n");  CudaTest("couldn't allocate minyd");
      if (cudaSuccess != cudaMalloc((void **)&minzl, sizeof(float) * blocks)) fprintf(stderr, "could not allocate minzd\n");  CudaTest("couldn't allocate minzd");

      if (cudaSuccess != cudaMemcpyToSymbol(nnodesd, &nnodes, sizeof(int))) fprintf(stderr, "copying of nnodes to device failed\n");  CudaTest("nnode copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(nbodiesd, &nbodies, sizeof(int))) fprintf(stderr, "copying of nbodies to device failed\n");  CudaTest("nbody copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(errd, &errl, sizeof(int))) fprintf(stderr, "copying of err to device failed\n");  CudaTest("err copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(dtimed, &dtime, sizeof(float))) fprintf(stderr, "copying of dtime to device failed\n");  CudaTest("dtime copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(dthfd, &dthf, sizeof(float))) fprintf(stderr, "copying of dthf to device failed\n");  CudaTest("dthf copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(epssqd, &epssq, sizeof(float))) fprintf(stderr, "copying of epssq to device failed\n");  CudaTest("epssq copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(itolsqd, &itolsq, sizeof(float))) fprintf(stderr, "copying of itolsq to device failed\n");  CudaTest("itolsq copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(sortd, &sortl, sizeof(int))) fprintf(stderr, "copying of sortl to device failed\n");  CudaTest("sortl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(countd, &countl, sizeof(int))) fprintf(stderr, "copying of countl to device failed\n");  CudaTest("countl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(startd, &startl, sizeof(int))) fprintf(stderr, "copying of startl to device failed\n");  CudaTest("startl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(childd, &childl, sizeof(int))) fprintf(stderr, "copying of childl to device failed\n");  CudaTest("childl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(massd, &massl, sizeof(int))) fprintf(stderr, "copying of massl to device failed\n");  CudaTest("massl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(posxd, &posxl, sizeof(int))) fprintf(stderr, "copying of posxl to device failed\n");  CudaTest("posxl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(posyd, &posyl, sizeof(int))) fprintf(stderr, "copying of posyl to device failed\n");  CudaTest("posyl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(poszd, &poszl, sizeof(int))) fprintf(stderr, "copying of poszl to device failed\n");  CudaTest("poszl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(velxd, &velxl, sizeof(int))) fprintf(stderr, "copying of velxl to device failed\n");  CudaTest("velxl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(velyd, &velyl, sizeof(int))) fprintf(stderr, "copying of velyl to device failed\n");  CudaTest("velyl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(velzd, &velzl, sizeof(int))) fprintf(stderr, "copying of velzl to device failed\n");  CudaTest("velzl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(accxd, &accxl, sizeof(int))) fprintf(stderr, "copying of accxl to device failed\n");  CudaTest("accxl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(accyd, &accyl, sizeof(int))) fprintf(stderr, "copying of accyl to device failed\n");  CudaTest("accyl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(acczd, &acczl, sizeof(int))) fprintf(stderr, "copying of acczl to device failed\n");  CudaTest("acczl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(maxxd, &maxxl, sizeof(int))) fprintf(stderr, "copying of maxxl to device failed\n");  CudaTest("maxxl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(maxyd, &maxyl, sizeof(int))) fprintf(stderr, "copying of maxyl to device failed\n");  CudaTest("maxyl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(maxzd, &maxzl, sizeof(int))) fprintf(stderr, "copying of maxzl to device failed\n");  CudaTest("maxzl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(minxd, &minxl, sizeof(int))) fprintf(stderr, "copying of minxl to device failed\n");  CudaTest("minxl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(minyd, &minyl, sizeof(int))) fprintf(stderr, "copying of minyl to device failed\n");  CudaTest("minyl copy to device failed");
      if (cudaSuccess != cudaMemcpyToSymbol(minzd, &minzl, sizeof(int))) fprintf(stderr, "copying of minzl to device failed\n");  CudaTest("minzl copy to device failed");
    }

    // generate input

    drndset(7);
    rsc = (3 * 3.1415926535897932384626433832795) / 16;
    vsc = sqrt(1.0 / rsc);
    for (i = 0; i < nbodies; i++) {
      mass[i] = 1.0 / nbodies;
      r = 1.0 / sqrt(pow(drnd()*0.999, -2.0/3.0) - 1);
      do {
        x = drnd()*2.0 - 1.0;
        y = drnd()*2.0 - 1.0;
        z = drnd()*2.0 - 1.0;
        sq = x*x + y*y + z*z;
      } while (sq > 1.0);
      scale = rsc * r / sqrt(sq);
      posx[i] = x * scale;
      posy[i] = y * scale;
      posz[i] = z * scale;

      do {
        x = drnd();
        y = drnd() * 0.1;
      } while (y > x*x * pow(1 - x*x, 3.5));
      v = x * sqrt(2.0 / sqrt(1 + r*r));
      do {
        x = drnd()*2.0 - 1.0;
        y = drnd()*2.0 - 1.0;
        z = drnd()*2.0 - 1.0;
        sq = x*x + y*y + z*z;
      } while (sq > 1.0);
      scale = vsc * v / sqrt(sq);
      velx[i] = x * scale;
      vely[i] = y * scale;
      velz[i] = z * scale;
    }

    if (cudaSuccess != cudaMemcpy(massl, mass, sizeof(float) * nbodies, cudaMemcpyHostToDevice)) fprintf(stderr, "copying of mass to device failed\n");  CudaTest("mass copy to device failed");
    if (cudaSuccess != cudaMemcpy(posxl, posx, sizeof(float) * nbodies, cudaMemcpyHostToDevice)) fprintf(stderr, "copying of posx to device failed\n");  CudaTest("posx copy to device failed");
    if (cudaSuccess != cudaMemcpy(posyl, posy, sizeof(float) * nbodies, cudaMemcpyHostToDevice)) fprintf(stderr, "copying of posy to device failed\n");  CudaTest("posy copy to device failed");
    if (cudaSuccess != cudaMemcpy(poszl, posz, sizeof(float) * nbodies, cudaMemcpyHostToDevice)) fprintf(stderr, "copying of posz to device failed\n");  CudaTest("posz copy to device failed");
    if (cudaSuccess != cudaMemcpy(velxl, velx, sizeof(float) * nbodies, cudaMemcpyHostToDevice)) fprintf(stderr, "copying of velx to device failed\n");  CudaTest("velx copy to device failed");
    if (cudaSuccess != cudaMemcpy(velyl, vely, sizeof(float) * nbodies, cudaMemcpyHostToDevice)) fprintf(stderr, "copying of vely to device failed\n");  CudaTest("vely copy to device failed");
    if (cudaSuccess != cudaMemcpy(velzl, velz, sizeof(float) * nbodies, cudaMemcpyHostToDevice)) fprintf(stderr, "copying of velz to device failed\n");  CudaTest("velz copy to device failed");

    // run timesteps (lauch GPU kernels)

    cudaEventCreate(&start);  cudaEventCreate(&stop);  
    starttime = clock();
    cudaEventRecord(start, 0);
    InitializationKernel<<<1, 1>>>();
    cudaEventRecord(stop, 0);  cudaEventSynchronize(stop);  cudaEventElapsedTime(&time, start, stop);
    timing[0] += time;
    CudaTest("kernel 0 launch failed");

    for (step = 0; step < timesteps; step++) {
      cudaEventRecord(start, 0);
      BoundingBoxKernel<<<blocks * FACTOR1, THREADS1>>>();
      cudaEventRecord(stop, 0);  cudaEventSynchronize(stop);  cudaEventElapsedTime(&time, start, stop);
      timing[1] += time;
      CudaTest("kernel 1 launch failed");

      cudaEventRecord(start, 0);
      TreeBuildingKernel<<<blocks * FACTOR2, THREADS2>>>();
      cudaEventRecord(stop, 0);  cudaEventSynchronize(stop);  cudaEventElapsedTime(&time, start, stop);
      timing[2] += time;
      CudaTest("kernel 2 launch failed");

      cudaEventRecord(start, 0);
      SummarizationKernel<<<blocks * FACTOR3, THREADS3>>>();
      cudaEventRecord(stop, 0);  cudaEventSynchronize(stop);  cudaEventElapsedTime(&time, start, stop);
      timing[3] += time;
      CudaTest("kernel 3 launch failed");

      cudaEventRecord(start, 0);
      SortKernel<<<blocks * FACTOR4, THREADS4>>>();
      cudaEventRecord(stop, 0);  cudaEventSynchronize(stop);  cudaEventElapsedTime(&time, start, stop);
      timing[4] += time;
      CudaTest("kernel 4 launch failed");

      cudaEventRecord(start, 0);
      ForceCalculationKernel<<<blocks * FACTOR5, THREADS5>>>();
      cudaEventRecord(stop, 0);  cudaEventSynchronize(stop);  cudaEventElapsedTime(&time, start, stop);
      timing[5] += time;
      CudaTest("kernel 5 launch failed");

      cudaEventRecord(start, 0);
      IntegrationKernel<<<blocks * FACTOR6, THREADS6>>>();
      cudaEventRecord(stop, 0);  cudaEventSynchronize(stop);  cudaEventElapsedTime(&time, start, stop);
      timing[6] += time;
      CudaTest("kernel 6 launch failed");
    }
    endtime = clock();
    CudaTest("kernel launch failed");
    cudaEventDestroy(start);  cudaEventDestroy(stop);

    // transfer result back to CPU
    if (cudaSuccess != cudaMemcpy(&error, errl, sizeof(int), cudaMemcpyDeviceToHost)) fprintf(stderr, "copying of err from device failed\n");  CudaTest("err copy from device failed");
    if (cudaSuccess != cudaMemcpy(posx, posxl, sizeof(float) * nbodies, cudaMemcpyDeviceToHost)) fprintf(stderr, "copying of posx from device failed\n");  CudaTest("posx copy from device failed");
    if (cudaSuccess != cudaMemcpy(posy, posyl, sizeof(float) * nbodies, cudaMemcpyDeviceToHost)) fprintf(stderr, "copying of posy from device failed\n");  CudaTest("posy copy from device failed");
    if (cudaSuccess != cudaMemcpy(posz, poszl, sizeof(float) * nbodies, cudaMemcpyDeviceToHost)) fprintf(stderr, "copying of posz from device failed\n");  CudaTest("posz copy from device failed");
    if (cudaSuccess != cudaMemcpy(velx, velxl, sizeof(float) * nbodies, cudaMemcpyDeviceToHost)) fprintf(stderr, "copying of velx from device failed\n");  CudaTest("velx copy from device failed");
    if (cudaSuccess != cudaMemcpy(vely, velyl, sizeof(float) * nbodies, cudaMemcpyDeviceToHost)) fprintf(stderr, "copying of vely from device failed\n");  CudaTest("vely copy from device failed");
    if (cudaSuccess != cudaMemcpy(velz, velzl, sizeof(float) * nbodies, cudaMemcpyDeviceToHost)) fprintf(stderr, "copying of velz from device failed\n");  CudaTest("velz copy from device failed");

    runtime = (int) (1000.0f * (endtime - starttime) / CLOCKS_PER_SEC);
    fprintf(stderr, "runtime: %d ms  (", runtime);
    time = 0;
    for (i = 1; i < 7; i++) {
      fprintf(stderr, " %.1f ", timing[i]);
      time += timing[i];
    }
    if (error == 0) {
      fprintf(stderr, ") = %.1f\n", time);
    } else {
      fprintf(stderr, ") = %.1f FAILED %d\n", time, error);
    }

    if ((run == 0) || (mintime > runtime)) mintime = runtime;
  }

  fprintf(stderr, "mintime: %d ms\n", mintime);

  // print output
//  for (i = 0; i < nbodies; i++) {
    printf("%.2e %.2e %.2e\n", posx[i], posy[i], posz[i]);
//  }

  free(mass);
  free(posx);
  free(posy);
  free(posz);
  free(velx);
  free(vely);
  free(velz);

  cudaFree(errl);
  cudaFree(childl);
  cudaFree(massl);
  cudaFree(posxl);
  cudaFree(posyl);
  cudaFree(poszl);
  cudaFree(countl);
  cudaFree(startl);

  cudaFree(maxxl);
  cudaFree(maxyl);
  cudaFree(maxzl);
  cudaFree(minxl);
  cudaFree(minyl);
  cudaFree(minzl);

  return 0;
}
