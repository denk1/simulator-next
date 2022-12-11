using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Network;
public class CarVision
{
    Material brickMaterial = null;
    Material detectedMaterial = null;
    GameObject _gameObject = null;
    SortedDictionary<string, float[]> dict_mat4;
    private byte[] bt_request_connection = new byte[] { 0x79, 0x91 };
    private byte[] bt_response_connection = new byte[] { 0x79, 0x90 };
    private byte[] bt_client_request = new byte[] { 0x79, 0x88 };
    private List<byte> obstacleBuffer;
    public byte[] bt_client_response = new byte[] { 0x79, 0x91 };
    public bool is_connected { get; set; }
    List<byte> _bufferListMat;
    private Server _server;
    private bool _is_running = true;
    private LayerMask _carLayerMask;
    private GameObject _carGameObject;
    private Transform _intersecCheckTransform;
    public CarVision(GameObject carGameObject, GameObject gameObject, Transform intersecCheckTransform, LayerMask carLayerMask)
    {
        brickMaterial = Resources.Load<Material>("Materials/Briks");
        detectedMaterial = Resources.Load<Material>("Materials/Detected");
        is_connected = false;
        dict_mat4 = new SortedDictionary<string, float[]>();
        _carGameObject = carGameObject;
        _gameObject = gameObject;
        _intersecCheckTransform = intersecCheckTransform;
        _carLayerMask = carLayerMask;
        _server = new Server(this, "127.0.0.1", 15555);
        _server.Start();
    }
    public bool checkResponseConnection(byte[] bt_header)
    {
        if (bt_header[0] == bt_response_connection[0] && bt_header[1] == bt_response_connection[1])
        {
            is_connected = true;
            return true;
        }
        return false;
    }

    public bool checkClientRequest(byte[] bt_header)
    {
        if (bt_header[0] == bt_client_request[0] && bt_header[1] == bt_client_request[1])
        {
            is_connected = true;
            return true;
        }
        return false;
    }

    public byte[] getRequestConnectionBytes()
    {
        return bt_request_connection;
    }

    public void updateObserving()
    {
        foreach (Transform child in _gameObject.transform)
        {
            GameObject childGameObject = child.gameObject;
            changeMaterial(childGameObject, brickMaterial);
        }

        Material testMaterial = brickMaterial;
        Collider[] hitColliders = Physics.OverlapBox(_intersecCheckTransform.position, new Vector3(50.0f, 2.0f, 50.0f), Quaternion.identity, _carLayerMask);
        dict_mat4.Clear();
        dict_mat4.Add("car", mat4x4toFloatArr( convertMatrixToRightHand(_carGameObject.transform.localToWorldMatrix)));
        foreach (Collider coll in hitColliders)
        {
            changeMaterial(coll.gameObject, detectedMaterial);
            dict_mat4.Add(coll.gameObject.name, mat4x4toFloatArr(convertMatrixToRightHand(coll.gameObject.transform.localToWorldMatrix)));
        }
        _server.fillBuffMat4(dict_mat4);
    }

    private void changeMaterial(GameObject gameObject, Material material)
    {
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshRenderer.material = material;
    }

    private void addColumn(float[] mat4x4, Vector4 vec4, int nCol)
    {
        int n = (nCol - 1) * 4;
        try
        {
            if (n < 0)
            {
                throw new Exception("Wrong index");
            }
            mat4x4[n] = vec4[0];
            mat4x4[n + 1] = vec4[1];
            mat4x4[n + 2] = vec4[2];
            mat4x4[n + 3] = vec4[3];
        }
        catch (Exception e)
        {
            Debug.Log("Wrong index");
        }
    }

    private float[] mat4x4toFloatArr(Matrix4x4 matTransform)
    {
        float[] mat4x4 = new float[16];
        Vector4 col1 = matTransform.GetRow(0);
        Vector4 col2 = matTransform.GetRow(1);
        Vector4 col3 = matTransform.GetRow(2);
        Vector4 col4 = matTransform.GetRow(3);
        addColumn(mat4x4, col1, 1);
        addColumn(mat4x4, col2, 2);
        addColumn(mat4x4, col3, 3);
        addColumn(mat4x4, col4, 4);
        return mat4x4;
    }

    public static byte[] floatToBytes(float f) 
    {
        return BitConverter.GetBytes(f);
    }

    public static byte[] intToBytes(UInt64 i) 
    {
        return BitConverter.GetBytes(i);
    }

    private Matrix4x4 convertMatrixToRightHand(Matrix4x4 mat4x4) 
    {
        Matrix4x4 Mi = Matrix4x4.identity;
        Mi[0, 0] = -1.0f;
        return  Mi * mat4x4 * Mi;
    } 
}
