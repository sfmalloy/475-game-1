﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject player;

    public GameObject mainCamera;

    private Vector3 offset = new Vector3(0f, 0f, -10f);

    public Transform getPlayerTransform()
    {
        return player.transform;
    }

    public void setPlayerTransform(Transform newTransform)
    {
        float xPos = newTransform.position.x;
        float yPos = newTransform.position.y;
        player.GetComponent<Transform>().position = new Vector3(xPos, yPos, 0.0f);
    }

    public void quitGame()
    {
        Application.Quit();
    }

    public void setCameraBounds(float x, float y, float xOffset, float yOffset)
    {
        mainCamera.GetComponent<CamFollow>().setBounds(x, y, xOffset, yOffset);
    }

    public void moveCameraToPosition(Transform pos)
    {
        mainCamera.GetComponent<Transform>().position = pos.position + offset;
    }
}
