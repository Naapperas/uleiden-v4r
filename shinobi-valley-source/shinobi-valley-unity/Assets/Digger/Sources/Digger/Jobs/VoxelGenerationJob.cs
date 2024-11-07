using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Digger
{
    [BurstCompile(CompileSynchronously = false)]
    public struct VoxelGenerationJob : IJobParallelFor
    {
        public int ChunkAltitude;

        [ReadOnly] public NativeArray<float> Heights;

        [WriteOnly] public NativeArray<Voxel> Voxels;

        public void Execute(int index)
        {
            // voxels[x * Size * Size + y * Size + z]
            var xi = index / Chunk.SizeVox2;
            var yi = (index - xi * Chunk.SizeVox2) / Chunk.SizeVox;
            var zi = index - xi * Chunk.SizeVox2 - yi * Chunk.SizeVox;
            var y = yi - 1;
            var height = Heights[xi * Chunk.SizeVox + zi];
            var voxel = new Voxel(y + ChunkAltitude - height, 0);
            Voxels[index] = voxel;
        }
    }
}