using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class PureCodeGameMenu : MonoBehaviour
{
    private GameObject loadingScreen;
    private Canvas canvas;
    private float loadingTime = 6f;
    private GameObject[] allObjects; // To store all other game objects
    private Camera tempCamera; // Temporary camera

    void Start()
    {
        // Pause the game and disable player input
        PauseGame();

        // Create Canvas
        canvas = new GameObject("Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler canvasScaler = canvas.gameObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        // Create and show the loading screen
        loadingScreen = CreateLoadingScreen();
        ShowLoadingScreen();

        // Create temporary camera for rendering the loading screen
        CreateTemporaryCamera();

        // Start the coroutine to hide the loading screen after a certain time
        StartCoroutine(HideLoadingScreenAfterDelay());
    }

    private GameObject CreateLoadingScreen()
    {
        GameObject screen = new GameObject("LoadingScreen");
        screen.transform.SetParent(canvas.transform);

        RectTransform rt = screen.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image image = screen.AddComponent<Image>();
        image.color = Color.black;

        GameObject textObject = new GameObject("LoadingText");
        textObject.transform.SetParent(screen.transform);
        Text loadingText = textObject.AddComponent<Text>();
        loadingText.text = "Loading...";
        loadingText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        loadingText.fontSize = 64;
        loadingText.alignment = TextAnchor.MiddleCenter;
        loadingText.color = Color.white;

        RectTransform textRT = textObject.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = Vector2.zero;
        textRT.sizeDelta = new Vector2(300, 100);

        return screen;
    }

    private void ShowLoadingScreen()
    {
        loadingScreen.SetActive(true);
    }

    private IEnumerator HideLoadingScreenAfterDelay()
    {
        // Wait for loadingTime seconds in real time (unaffected by Time.timeScale)
        yield return new WaitForSecondsRealtime(loadingTime);

        // Re-enable all objects and unpause the game
        ResumeGame();

        // Destroy the temporary camera
        Destroy(tempCamera.gameObject);

        // Hide the loading screen
        loadingScreen.SetActive(false);
    }

    private void PauseGame()
    {
        // Set time scale to 0 to pause the game
        Time.timeScale = 0f;

        // Find all game objects in the scene
        allObjects = FindObjectsOfType<GameObject>();

        // Disable all game objects except the loading screen and this script's game object
        foreach (GameObject obj in allObjects)
        {
            if (obj != this.gameObject && obj != loadingScreen)
            {
                obj.SetActive(false);
            }
        }

        // Disable player input
        if (Keyboard.current != null)
        {
            InputSystem.DisableDevice(Keyboard.current);
        }

        if (Mouse.current != null)
        {
            InputSystem.DisableDevice(Mouse.current);
        }
    }

    private void ResumeGame()
    {
        // Re-enable all game objects
        foreach (GameObject obj in allObjects)
        {
            if (obj != this.gameObject && obj != loadingScreen)
            {
                obj.SetActive(true);
            }
        }

        // Unpause the game by setting time scale back to normal
        Time.timeScale = 1f;

        // Enable player input again
        if (Keyboard.current != null)
        {
            InputSystem.EnableDevice(Keyboard.current);
        }

        if (Mouse.current != null)
        {
            InputSystem.EnableDevice(Mouse.current);
        }
    }

    private void CreateTemporaryCamera()
    {
        // Create a new camera
        GameObject cameraObject = new GameObject("TemporaryCamera");
        tempCamera = cameraObject.AddComponent<Camera>();

        // Set the camera's clear flags to ensure it only shows the UI
        tempCamera.clearFlags = CameraClearFlags.SolidColor;
        tempCamera.backgroundColor = Color.black;

        // Set the camera to render the UI layer
        tempCamera.cullingMask = LayerMask.GetMask("UI");

        // Set the camera's depth to be higher than other cameras so it renders on top
        tempCamera.depth = 100;
    }
}
