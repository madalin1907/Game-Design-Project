using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Direction {
    
    public static int NORTH = 0;
    public static int EAST = 1;
    public static int SOUTH = 2;
    public static int WEST = 3;

    public static int GetOppositeDirection(int dir) {
        if (dir == NORTH) return SOUTH;
        if (dir == EAST) return WEST;
        if (dir == SOUTH) return NORTH;
        if (dir == WEST) return EAST;
        return -1;
    }

    public static Vector2Int GetOffset(int dir) {
        if (dir == NORTH) return new Vector2Int(0, 1);
        if (dir == EAST) return new Vector2Int(1, 0);
        if (dir == SOUTH) return new Vector2Int(0, -1);
        if (dir == WEST) return new Vector2Int(-1, 0);
        return new Vector2Int(0, 0);
    }

}
