﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour {
    public GameObject FPSCanvas;
    public Camera firstperson;
    public Camera thirdperson;
    public Camera freecam;
    public Transform uiSlider;
    //public GameObject UIElements;
    public PlayerPossession possession;
    public enum CameraMode {
        FirstPerson = 0,
        ThirdPerson,
        FreeCam,
        FreeCamLocked,
    }
    public CameraMode mode = CameraMode.FirstPerson;
    public void Start() {
        SwitchCamera(CameraMode.FirstPerson);
    }
    public void Update() {
        uiSlider.transform.localPosition = Vector3.Lerp(uiSlider.transform.localPosition, -Vector3.right * 30f * ((int)mode+0.5f), Time.deltaTime*2f);
    }

    public void OnSwitchCamera() {
        int index = ((int)mode + 1) % 4;
        SwitchCamera((CameraMode)index);
    }

    public void OnFirstPerson() {
        SwitchCamera(CameraMode.FirstPerson);
    }
    public void OnThirdPerson() {
        SwitchCamera(CameraMode.ThirdPerson);
    }
    public void OnFreeCamera() {
        SwitchCamera(CameraMode.FreeCam);
    }
    public void OnLockedCamera() {
        SwitchCamera(CameraMode.FreeCamLocked);
    }

    public void SwitchCamera(CameraMode cameraMode) {
        mode = cameraMode;
        firstperson.enabled = false;
        //firstperson.GetComponent<AudioListener>().enabled = false;
        thirdperson.enabled = false;
        //thirdperson.GetComponent<AudioListener>().enabled = false;
        freecam.enabled = false;
        //freecam.GetComponent<AudioListener>().enabled = false;
        possession.enabled = true;

        freecam.GetComponent<SimpleCameraController>().enabled = false;
        switch (mode) {
            case CameraMode.FirstPerson:
                firstperson.enabled = true;
                //firstperson.GetComponent<AudioListener>().enabled = true;
                if (!FPSCanvas.activeInHierarchy) {
                    FPSCanvas.SetActive(true);
                }
                break;
            case CameraMode.ThirdPerson:
                thirdperson.enabled = true;
                //thirdperson.GetComponent<AudioListener>().enabled = true;
                if (!FPSCanvas.activeInHierarchy) {
                    FPSCanvas.SetActive(true);
                }
                break;
            case CameraMode.FreeCam:
                freecam.enabled = true;
                freecam.GetComponent<SimpleCameraController>().enabled = true;
                //freecam.GetComponent<AudioListener>().enabled = true;
                possession.enabled = false;
                possession.controller.inputDir = Vector3.zero;
                possession.controller.inputJump = false;
                if (FPSCanvas.activeInHierarchy) {
                    FPSCanvas.SetActive(false);
                }
                break;
            case CameraMode.FreeCamLocked:
                freecam.enabled = true;
                freecam.GetComponent<SimpleCameraController>().enabled = false;
                //freecam.GetComponent<AudioListener>().enabled = true;
                possession.enabled = true;
                if (!FPSCanvas.activeInHierarchy) {
                    FPSCanvas.SetActive(true);
                }
                break;
        }

    }
}
