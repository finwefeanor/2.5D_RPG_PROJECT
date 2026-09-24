using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    private GameObject pauseMenuCanvas;
    private bool isPaused = false;

    private InventoryManager inventoryManager;
    private EquipmentManager equipmentManager;

    void Start()
    {
        var refs = GameManager.Instance != null ? GameManager.Instance.Player : null;
        if (refs != null)
        {
            inventoryManager = refs.Inventory;
            equipmentManager = refs.Equipment;
        }

        var ui = PauseUIRefs.Instance;
        if (ui != null) pauseMenuCanvas = ui.pausePanel;
        else Debug.LogWarning("PauseMenu: no PauseMenuCanvas in scene.", this);

        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f; // freezes all Update-based movement/physics, but not UI
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(true);
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);
    }

    // Wire this to the Save button
    public void OnSaveButton()
    {
        SaveSystem.Save(inventoryManager, equipmentManager);
    }

    public void OnNewGameButton()
    {
        SaveSystem.ClearSave();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Wire this to the Restart button
    public void OnRestartButton()
    {
        Time.timeScale = 1f; // must reset before reload, or the new scene loads frozen
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Wire this to the Quit button
    public void OnQuitButton()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
