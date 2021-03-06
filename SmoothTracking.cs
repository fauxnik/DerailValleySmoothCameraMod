﻿using AwesomeTechnologies.VegetationSystem;
using System;
using UnityEngine;
using UnityEngine.XR;

namespace SmoothCamera
{
    class SmoothTracking : MonoBehaviour
    {
        static GameViewRenderMode defaultGameViewRenderMode;

        public static void SetupSmoothedCamera()
        {
            if (!VRManager.IsVREnabled())
            {
                Main.LogWarning("[SmoothCamera] >>> Game running in non-VR mode. Skipping camera setup.");
                return;
            }

            if (GameObject.Find("SmoothCamera") != null)
            {
                Main.LogWarning("[SmoothCamera] >>> SmoothCamera already exists. Skipping camera setup.");
                return;
            }

            var veggieSys = UnityEngine.Object.FindObjectOfType<VegetationSystemPro>();
            if (Camera.main == null || veggieSys == null)
            {
                Main.Log("[SmoothCamera] >>> Delaying camera setup...");
                WorldStreamingInit.LoadingFinished += MainCamSetup;
                return;
            }

            MainCamSetup();
        }

        public static void TeardownSmoothedCamera()
        {
            Main.Log("[SmoothCamera] >>> Tearing down camera...");

            var smoothCam = GameObject.Find("SmoothCamera");
            if (smoothCam == null)
            {
                Main.LogWarning("[SmoothCamera] >>> SmoothCamera not found. Skipping camera teardown.");
                return;
            }

            VegetationSystemPro veggieSys = UnityEngine.Object.FindObjectOfType<VegetationSystemPro>();
            veggieSys.RemoveCamera(smoothCam.GetComponent<Camera>());
            XRSettings.gameViewRenderMode = defaultGameViewRenderMode;

            Destroy(smoothCam);
        }

        static void MainCamSetup()
        {
            Main.Log("[SmoothCamera] >>> Setting up main camera...");

            var veggieSys = UnityEngine.Object.FindObjectOfType<VegetationSystemPro>();
            var smoothCam = SetupCamera("SmoothCamera", Camera.main);
            veggieSys.AddCamera(smoothCam);
            defaultGameViewRenderMode = XRSettings.gameViewRenderMode;
            XRSettings.gameViewRenderMode = GameViewRenderMode.None;
        }

        static Camera SetupCamera(string name, Camera target)
        {
            if (target == null) { return null; }
            var smoothCam = new GameObject { name = name }.AddComponent<Camera>();
            smoothCam.CopyFrom(target);
            smoothCam.stereoTargetEye = StereoTargetEyeMask.None;
            smoothCam.depth = target.depth + 20;
            smoothCam.gameObject.AddComponent<SmoothTracking>().trackedCam = target;
            return smoothCam;
        }

        void Initialize()
        {
            initialized = true;
            transform.position = trackedCam.transform.position;
            transform.rotation = trackedCam.transform.rotation;
            positionVelo = Vector3.zero;
            rotationVelo = Vector3.zero;
            lastUpdate = DateTime.Now;
            SingletonBehaviour<WorldMover>.Instance.WorldMoved += OnWorldMoved;
        }

        void OnDestroy()
        {
            SingletonBehaviour<WorldMover>.Instance.WorldMoved -= OnWorldMoved;
        }

        void OnWorldMoved(WorldMover _, Vector3 moveVector)
        {
            transform.position = transform.position - moveVector;
        }

        void Update()
        {
            if (trackedCam == null) { return; }

            if (!initialized) { Initialize(); }

            var smoothTimePosition = Main.settings.smoothTimePosition;
            var smoothTimeRotation = Main.settings.smoothTimeRotation;
            var now = DateTime.Now;
            var deltaTime = (float)(now - lastUpdate).TotalSeconds;
            lastUpdate = now;

            transform.position
                = Vector3.SmoothDamp(transform.position, trackedCam.transform.position, ref positionVelo, smoothTimePosition, float.PositiveInfinity, deltaTime);

            var prevEuler = transform.rotation.eulerAngles;
            var trackedCamEuler = trackedCam.transform.rotation.eulerAngles;
            var nextEuler = new Vector3(
                Mathf.SmoothDampAngle(prevEuler.x, trackedCamEuler.x, ref rotationVelo.x, smoothTimeRotation, float.PositiveInfinity, deltaTime),
                Mathf.SmoothDampAngle(prevEuler.y, trackedCamEuler.y, ref rotationVelo.y, smoothTimeRotation, float.PositiveInfinity, deltaTime),
                Mathf.SmoothDampAngle(prevEuler.z, trackedCamEuler.z, ref rotationVelo.z, smoothTimeRotation, float.PositiveInfinity, deltaTime));
            transform.rotation = Quaternion.Euler(nextEuler);

            GetComponent<Camera>().fieldOfView = Main.settings.fieldOfView / XRDevice.fovZoomFactor;
        }

        public Camera trackedCam;

        Vector3 positionVelo;
        Vector3 rotationVelo;
        DateTime lastUpdate;
        bool initialized = false;
    }
}
