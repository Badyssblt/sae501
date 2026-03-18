/*
 Logique pour l'affichage des highscores en partie.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Anatidae {
    public class HighscoreUI : MonoBehaviour
    {
        [SerializeField] RectTransform highscoreEntryContainer;
        [SerializeField] GameObject highscoreEntryPrefab;
        [SerializeField] RectTransform viewport;
        [SerializeField][Tooltip("Nombre de champs à afficher")] int numHighscoreEntries = 10;
        [SerializeField][Tooltip("Rendre le premier score plus gros")] bool makeFirstBigger = true;
        [SerializeField][Tooltip("Défiler les scores de haut en bas automatiquement")] bool autoscroll = false;

        [Header("Bouton Restart")]
        [SerializeField] private Button restartButton;
        [SerializeField] private ColorBlock selectedColors;
        [SerializeField] private ColorBlock normalColors;

        private bool isButtonSelected = false;
        private bool initialized = false;
        private float inputDelay = 0.2f;
        private float lastInputTime = 0f;

        private void InitIfNeeded()
        {
            if (initialized) return;
            initialized = true;

            if (normalColors.normalColor == Color.clear)
            {
                normalColors = ColorBlock.defaultColorBlock;
            }
            if (selectedColors.normalColor == Color.clear)
            {
                selectedColors = ColorBlock.defaultColorBlock;
                selectedColors.normalColor = Color.yellow;
                selectedColors.highlightedColor = Color.yellow;
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(false);
                restartButton.onClick.AddListener(OnRestart);
            }
        }

        public void OnEnable()
        {
            InitIfNeeded();
            isButtonSelected = false;
            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(true);
                restartButton.colors = normalColors;
            }
            StartCoroutine(Init());
        }

        IEnumerator Init()
        {
            Debug.Log("Récupération des highscores...", this);
            yield return HighscoreManager.FetchHighscores();
            UpdateHighscoreEntries();
        }

        void Update()
        {
            HandleJoystickInput();
        }

        private void HandleJoystickInput()
        {
            if (restartButton == null) return;
            if (Time.time - lastInputTime < inputDelay) return;

            float vertical = Input.GetAxisRaw("P1_Vertical");

            // Bas → sélectionner le bouton
            if (vertical < -0.5f && !isButtonSelected)
            {
                isButtonSelected = true;
                restartButton.colors = selectedColors;
                lastInputTime = Time.time;
            }
            // Haut → désélectionner le bouton
            else if (vertical > 0.5f && isButtonSelected)
            {
                isButtonSelected = false;
                restartButton.colors = normalColors;
                lastInputTime = Time.time;
            }

            // Confirmer avec P1_B1
            if (isButtonSelected && Input.GetButtonDown("P1_B1"))
            {
                OnRestart();
            }
        }

        private void OnRestart()
        {
            // Ne pas restart sur les clients web
            if (NetworkManager.Instance?.Role == CookMoiCa.Network.NetworkRole.Client)
                return;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void UpdateHighscoreEntries()
        {
            float prefabHeight = highscoreEntryPrefab.GetComponent<RectTransform>().sizeDelta.y;
            foreach (Transform child in highscoreEntryContainer.transform)
            {
                Destroy(child.gameObject);
            }

            if (HighscoreManager.Highscores == null) return;

            int i = 0;
            foreach (HighscoreManager.HighscoreEntry entry in HighscoreManager.Highscores)
            {
                GameObject entryGo = Instantiate(highscoreEntryPrefab, highscoreEntryContainer);
                entryGo.transform.localPosition = new Vector3(
                    0f,
                    -i * prefabHeight + 10f,
                    0f
                );
                HighscoreEntryGo highscoreEntry = entryGo.GetComponent<HighscoreEntryGo>();
                highscoreEntry.SetData(entry);
                if (makeFirstBigger && i == 0)
                    highscoreEntry.SetScale(1.3f);
                i++;
                if (i >= numHighscoreEntries)
                    break;
            }

            highscoreEntryContainer.sizeDelta = new Vector2(
                highscoreEntryContainer.sizeDelta.x,
                i * 50
            );
        }
    }
}
