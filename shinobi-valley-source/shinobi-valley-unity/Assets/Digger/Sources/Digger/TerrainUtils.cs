using Unity.Mathematics;
using UnityEngine;

namespace Digger
{
    public static class TerrainUtils
    {
        public static Vector3i TerrainRelativePositionToAlphamapPosition(TerrainData terrainData, Vector3 terrainRelativePosition)
        {
            return new Vector3i(terrainRelativePosition.x / terrainData.size.x * terrainData.alphamapWidth,
                                terrainRelativePosition.y,
                                terrainRelativePosition.z / terrainData.size.z * terrainData.alphamapHeight);
        }

        public static int2 AlphamapPositionToDetailMapPosition(TerrainData terrainData, int x, int y)
        {
            return new int2(
                x * terrainData.detailWidth / terrainData.alphamapWidth,
                y * terrainData.detailHeight / terrainData.alphamapHeight
            );
        }
    }
}