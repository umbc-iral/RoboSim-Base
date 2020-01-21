//MIT License
//Copyright 2016-Present 
//Ross Tredinnick
//Brady Boettcher
//Living Environments Laboratory
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), 
//to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, 
//sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
//INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ProjectionPlane : MonoBehaviour {

    public GameObject cameraRig;
    public int width;
    public int height;
    public int x;
    public int y;
    public Vector3 eyeOffset;

    private GameObject leftCamera;
    private GameObject rightCamera;
    private Camera leftCameraComponent;
    private Camera rightCameraComponent;
    private Vector3 pa, pb, pc;

    private int windowOffsetX = 0;
    private int windowOffsetY = 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
        public static implicit operator Vector2(POINT p)
        {
            return new Vector2(p.X, p.Y);
        }
    }
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetCursorPos(out POINT lpPoint);

    void refreshViewport()
    {
        POINT p;
        GetCursorPos(out p);
        int currentX = p.X - (int)Input.mousePosition.x;
        int currentY = p.Y - (Screen.height - (int)Input.mousePosition.y);
        if (currentX != windowOffsetX || currentY != windowOffsetY)
        {
            windowOffsetX = currentX;
            windowOffsetY = currentY;
            Rect rect = new Rect((x - (float)windowOffsetX) / Screen.width, (y - (float)windowOffsetY) / Screen.height, (float)width / Screen.width, (float)height / Screen.height);
            leftCamera.GetComponent<Camera>().rect = rect;
            rightCamera.GetComponent<Camera>().rect = rect;
        }
    }

    void Start()
    {
        leftCamera = new GameObject();
        rightCamera = new GameObject();
        leftCameraComponent = leftCamera.AddComponent<Camera>();
        rightCameraComponent = rightCamera.AddComponent<Camera>();
        leftCamera.transform.parent = cameraRig.transform;
        rightCamera.transform.parent = cameraRig.transform;
        leftCameraComponent.stereoTargetEye = StereoTargetEyeMask.Left;
        rightCameraComponent.stereoTargetEye = StereoTargetEyeMask.Right;
        leftCamera.transform.position = cameraRig.transform.position + eyeOffset / 2;
        rightCamera.transform.position = cameraRig.transform.position + eyeOffset / -2;

        refreshViewport();
    }

    void LateUpdate () {
        refreshViewport();
        
        pa = transform.TransformPoint(GetComponent<MeshFilter>().mesh.vertices[0]);
        pb = transform.TransformPoint(GetComponent<MeshFilter>().mesh.vertices[1]);
        pc = transform.TransformPoint(GetComponent<MeshFilter>().mesh.vertices[2]);

        leftCamera.transform.rotation = transform.rotation;
        rightCamera.transform.rotation = transform.rotation;

        updateAsymProjMatrix(pa, pb, pc, cameraRig.transform.position, leftCameraComponent, Camera.StereoscopicEye.Left);
        updateAsymProjMatrix(pa, pb, pc, cameraRig.transform.position, rightCameraComponent, Camera.StereoscopicEye.Right);
    }

    void updateAsymProjMatrix(Vector3 pa, Vector3 pb, Vector3 pc, Vector3 pe, Camera cam, Camera.StereoscopicEye eye)
    {
        //compute orthonormal basis for the screen - could pre-compute this...
        Vector3 vr = (pb - pa).normalized;
        Vector3 vu = (pc - pa).normalized;
        Vector3 vn = Vector3.Cross(vr, vu).normalized;

        //compute screen corner vectors
        Vector3 va = pa - pe;
        Vector3 vb = pb - pe;
        Vector3 vc = pc - pe;

        //find the distance from the eye to screen plane
        float n = cam.nearClipPlane;
        float f = cam.farClipPlane;
        float d = Vector3.Dot(va, vn); // distance from eye to screen
		float nod = n / d;
        float l = Vector3.Dot(vr, va) * nod;
        float r = Vector3.Dot(vr, vb) * nod;
        float b = Vector3.Dot(vu, va) * nod;
        float t = Vector3.Dot(vu, vc) * nod;

        //put together the matrix - bout time amirite?
        Matrix4x4 m = Matrix4x4.zero;

        //from http://forum.unity3d.com/threads/using-projection-matrix-to-create-holographic-effect.291123/
        m[0, 0] = 2.0f * n / (r - l);
        m[0, 2] = (r + l) / (r - l);
        m[1, 1] = 2.0f * n / (t - b);
        m[1, 2] = (t + b) / (t - b);
        m[2, 2] = -(f + n) / (f - n);
        m[2, 3] = (-2.0f * f * n) / (f - n);
        m[3, 2] = -1.0f;

        cam.projectionMatrix = m;
        cam.SetStereoProjectionMatrix(eye, m);
    } 
}
