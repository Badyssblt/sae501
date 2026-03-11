using System.Collections.Generic;
using UnityEngine;
using CookMoiCa.Network;

namespace CookMoiCa.Network
{
    /// <summary>
    /// Système de prediction côté client.
    /// - Stocke les inputs non confirmés
    /// - Détecte les désynchronisations avec le serveur
    /// - Effectue le rollback et replay si nécessaire
    /// </summary>
    public class PredictionSystem
    {
        private readonly List<InputSnapshot> pendingInputs = new List<InputSnapshot>();
        private readonly int maxPendingInputs;
        private readonly float positionTolerance;

        private uint lastConfirmedTick = 0;
        private StateSnapshot lastConfirmedState;

        // Statistiques pour debug
        public int PendingInputCount => pendingInputs.Count;
        public int RollbackCount { get; private set; } = 0;

        public PredictionSystem(int maxPending = 60, float posTolerance = 0.5f)
        {
            maxPendingInputs = maxPending;
            positionTolerance = posTolerance;
        }

        /// <summary>
        /// Ajoute un input à la liste des inputs en attente de confirmation
        /// </summary>
        public void AddPendingInput(InputSnapshot input)
        {
            pendingInputs.Add(input);

            // Limiter la taille (évite les memory leaks si déconnexion)
            while (pendingInputs.Count > maxPendingInputs)
            {
                pendingInputs.RemoveAt(0);
                Debug.LogWarning("[Prediction] Trop d'inputs en attente, suppression des anciens");
            }
        }

        /// <summary>
        /// Vérifie si l'état du serveur correspond à notre prédiction locale
        /// Retourne true si rollback nécessaire
        /// </summary>
        public bool CheckForDesync(StateSnapshot serverState, int localPlayerId, Vector2 localPosition, string localCarry)
        {
            if (serverState == null) return false;

            // Mettre à jour le dernier état confirmé
            lastConfirmedTick = serverState.Tick;
            lastConfirmedState = serverState;

            // Supprimer les inputs déjà traités par le serveur
            pendingInputs.RemoveAll(i => i.Tick <= serverState.Tick);

            // Vérifier si le joueur local existe dans l'état serveur
            if (!serverState.Players.ContainsKey(localPlayerId))
            {
                return false;
            }

            var serverPlayer = serverState.Players[localPlayerId];
            Vector2 serverPos = new Vector2(serverPlayer.x, serverPlayer.y);

            // Vérifier la position
            float positionDiff = Vector2.Distance(localPosition, serverPos);
            if (positionDiff > positionTolerance)
            {
                Debug.Log($"[Prediction] Désync position détectée: local={localPosition}, server={serverPos}, diff={positionDiff}");
                RollbackCount++;
                return true;
            }

            // Vérifier l'inventaire (null et "" sont équivalents = pas d'item)
            string normalizedLocal = string.IsNullOrEmpty(localCarry) ? null : localCarry;
            string normalizedServer = string.IsNullOrEmpty(serverPlayer.carry) ? null : serverPlayer.carry;
            if (normalizedLocal != normalizedServer)
            {
                Debug.Log($"[Prediction] Désync inventaire détecté: local={normalizedLocal ?? "null"}, server={normalizedServer ?? "null"}");
                RollbackCount++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retourne l'état confirmé par le serveur pour restauration
        /// </summary>
        public StateSnapshot GetConfirmedState()
        {
            return lastConfirmedState;
        }

        /// <summary>
        /// Retourne la liste des inputs à rejouer après rollback
        /// </summary>
        public List<InputSnapshot> GetInputsToReplay()
        {
            return new List<InputSnapshot>(pendingInputs);
        }

        /// <summary>
        /// Retourne le dernier tick confirmé par le serveur
        /// </summary>
        public uint GetLastConfirmedTick()
        {
            return lastConfirmedTick;
        }

        /// <summary>
        /// Nettoie les inputs trop vieux
        /// </summary>
        public void CleanOldInputs(uint currentTick, uint maxAge = 60)
        {
            if (currentTick > maxAge)
            {
                uint cutoffTick = currentTick - maxAge;
                pendingInputs.RemoveAll(i => i.Tick < cutoffTick);
            }
        }

        /// <summary>
        /// Vide tous les inputs en attente
        /// </summary>
        public void Clear()
        {
            pendingInputs.Clear();
            lastConfirmedTick = 0;
            lastConfirmedState = null;
        }

        /// <summary>
        /// Vérifie si un tick donné a déjà été confirmé
        /// </summary>
        public bool IsTickConfirmed(uint tick)
        {
            return tick <= lastConfirmedTick;
        }
    }

    /// <summary>
    /// Helper pour effectuer le rollback et replay
    /// </summary>
    public static class RollbackHelper
    {
        /// <summary>
        /// Calcule la nouvelle position après avoir rejoué les inputs
        /// </summary>
        public static Vector2 ReplayMovementInputs(Vector2 startPosition, List<InputSnapshot> inputs, float moveSpeed, float deltaTime)
        {
            Vector2 position = startPosition;

            foreach (var input in inputs)
            {
                Vector2 movement = input.Movement.normalized;
                position += movement * moveSpeed * deltaTime;
            }

            return position;
        }

        /// <summary>
        /// Vérifie si deux positions sont considérées comme égales
        /// </summary>
        public static bool PositionsMatch(Vector2 a, Vector2 b, float tolerance = 0.5f)
        {
            return Vector2.Distance(a, b) <= tolerance;
        }

        /// <summary>
        /// Interpole doucement vers la position cible (pour rollback moins brutal)
        /// </summary>
        public static Vector2 SmoothCorrection(Vector2 current, Vector2 target, float smoothTime = 0.1f)
        {
            return Vector2.Lerp(current, target, Time.deltaTime / smoothTime);
        }
    }
}
