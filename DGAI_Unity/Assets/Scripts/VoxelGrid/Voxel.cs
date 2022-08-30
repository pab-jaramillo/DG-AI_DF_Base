using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// VoxelState represents the current state of a Voxel
/// </summary>
public enum VoxelState { White, Black, Red, Yellow, Empty, NotUsed };


public class Voxel : IEquatable<Voxel>
{
    #region Campos públicos

    public Vector3Int Index;
    public List<Face> Faces = new List<Face>(6);
    public Vector3 Center => (Index + _voxelGrid.Origin) * _size;
    public VoxelState State { get; private set; }

    #endregion

    #region Private Fields

    VoxelGrid _voxelGrid;
    float _size;
    GameObject _voxelGO;

    #endregion

    #region Builders

    /// <summary>
    /// Create a Voxel in a <see cref="VoxelGrid"/>
    /// </summary>
    /// <param name="index">The Voxel index</param>
    /// <param name="voxelgrid">The <see cref="VoxelGrid"/> where this <see cref="Voxel"/> is located</param>
    /// <param name="voxelGameObject">The <see cref="GameObject"/> that represents this</param>
    public Voxel(Vector3Int index, VoxelGrid voxelGrid, GameObject prefab, VoxelState state = VoxelState.Empty, Transform parent = null, float sizeFactor = 1f)
    {
        Index = index;
        _voxelGrid = voxelGrid;
        _size = _voxelGrid.VoxelSize;
        _voxelGO = GameObject.Instantiate(prefab, index, Quaternion.identity, parent);
        _voxelGO.transform.localScale = Vector3.one * sizeFactor;
        var trigger = _voxelGO.AddComponent<VoxelTrigger>();
        trigger.Voxel = this;
        SetState(state);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Destroys the <see cref="GameObject"/> associated with this <see cref="Voxel"/>
    /// </summary>
    public void DestroyGO()
    {
        GameObject.Destroy(_voxelGO);
    }

    /// <summary>
    /// Collect the neighbors of this <see cref="Voxel"/> on each face, if you have one
    /// </summary>
    /// <returns>The neighboring voxels</returns>
    public IEnumerable<Voxel> GetFaceNeighbours()
    {
        int x = Index.x;
        int y = Index.y;
        int z = Index.z;
        var s = _voxelGrid.Size;

        if (x != s.x - 1) yield return _voxelGrid.Voxels[x + 1, y, z];
        if (x != 0) yield return _voxelGrid.Voxels[x - 1, y, z];

        if (y != s.y - 1) yield return _voxelGrid.Voxels[x, y + 1, z];
        if (y != 0) yield return _voxelGrid.Voxels[x, y - 1, z];

        if (z != s.z - 1) yield return _voxelGrid.Voxels[x, y, z + 1];
        if (z != 0) yield return _voxelGrid.Voxels[x, y, z - 1];
    }

    /// <summary>
    /// Collect the neighbors of this <see cref="Voxel"/> on each face, if any, in
    /// of array of size 6
    /// </summary>
    /// <returns></returns>
    public Voxel[] GetFaceNeighboursArray()
    {
        Voxel[] result = new Voxel[6];

        int x = Index.x;
        int y = Index.y;
        int z = Index.z;
        var s = _voxelGrid.Size;

        if (x != s.x - 1) result[0] = _voxelGrid.Voxels[x + 1, y, z];
        else result[0] = null;

        if (x != 0) result[1] = _voxelGrid.Voxels[x - 1, y, z];
        else result[1] = null;

        if (y != s.y - 1) result[2] = _voxelGrid.Voxels[x, y + 1, z];
        else result[2] = null;

        if (y != 0) result[3] = _voxelGrid.Voxels[x, y - 1, z];
        else result[3] = null;

        if (z != s.z - 1) result[4] = _voxelGrid.Voxels[x, y, z + 1];
        else result[4] = null;

        if (z != 0) result[5] = _voxelGrid.Voxels[x, y, z - 1];
        else result[5] = null;

        return result;
    }

    /// <summary>
    /// Change the state of this voxel to a new one <see cref="VoxelState"/>
    /// </summary>
    /// <param name="newState"></param>
    public void SetState(VoxelState newState)
    {
        State = newState;
        var renderer = _voxelGO.GetComponent<MeshRenderer>();
        var collider = _voxelGO.GetComponent<Collider>();
        if (State == VoxelState.White)
        {
            renderer.enabled = true;
            collider.enabled = true;
            renderer.material = Resources.Load<Material>("Materials/White");
        }
        else if (State == VoxelState.Black)
        {
            renderer.enabled = true;
            collider.enabled = true;
            renderer.material = Resources.Load<Material>("Materials/Black");
        }
        else if (State == VoxelState.Yellow)
        {
            renderer.enabled = true;
            collider.enabled = true;
            renderer.material = Resources.Load<Material>("Materials/Yellow");
        }
        else if (State == VoxelState.Red)
        {
            renderer.enabled = true;
            collider.enabled = true;
            renderer.material = Resources.Load<Material>("Materials/Red");
        }
        else if (State == VoxelState.Empty || State == VoxelState.NotUsed)
        {
            renderer.enabled = false;
            collider.enabled = false;

        }
    }

    #endregion

    #region Equality checks

    /// <summary>
    /// Checks if two voxels are equal based on their Index
    /// </summary>
    /// <param name="other">The <see cref="Voxel"/> to compare with</param>
    /// <returns>True if the Voxels are equal</returns>
    public bool Equals(Voxel other)
    {
        return (other != null) && (Index == other.Index);
    }

    /// <summary>
    /// Get the HashCode of this <see cref="Voxel"/> based on its Index
    /// </summary>
    /// <returns>The HashCode as an Int</returns>
    public override int GetHashCode()
    {
        return Index.GetHashCode();
    }

    #endregion
}