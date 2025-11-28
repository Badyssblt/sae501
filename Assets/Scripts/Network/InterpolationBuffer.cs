using System.Collections.Generic;
using UnityEngine;
using CookMoiCa.Network;

namespace CookMoiCa.Network
{
    /// <summary>
    /// Buffer d'interpolation pour lisser les mouvements des entités distantes.
    /// Stocke les snapshots reçus et interpole entre eux avec un délai de 100ms.
    /// </summary>
    public class InterpolationBuffer
    {
        private readonly List<StateSnapshot> snapshots = new List<StateSnapshot>();
        private readonly float interpolationDelay;
        private readonly int maxSnapshots;

        public InterpolationBuffer(float interpolationDelaySeconds = 0.1f, int maxSnapshotsCount = 30)
        {
            interpolationDelay = interpolationDelaySeconds;
            maxSnapshots = maxSnapshotsCount;
        }

        /// <summary>
        /// Ajoute un nouveau snapshot au buffer
        /// </summary>
        public void AddSnapshot(StateSnapshot snapshot)
        {
            // Insérer dans l'ordre chronologique
            int insertIndex = snapshots.Count;
            for (int i = snapshots.Count - 1; i >= 0; i--)
            {
                if (snapshots[i].Tick < snapshot.Tick)
                {
                    insertIndex = i + 1;
                    break;
                }
                if (snapshots[i].Tick == snapshot.Tick)
                {
                    // Snapshot déjà présent, mettre à jour
                    snapshots[i] = snapshot;
                    return;
                }
                insertIndex = i;
            }

            snapshots.Insert(insertIndex, snapshot);

            // Limiter la taille du buffer
            while (snapshots.Count > maxSnapshots)
            {
                snapshots.RemoveAt(0);
            }
        }

        /// <summary>
        /// Retourne la position interpolée pour un joueur à un moment donné
        /// </summary>
        public Vector2? GetInterpolatedPosition(int playerId, float currentTime)
        {
            float renderTime = currentTime - interpolationDelay;

            // Trouver les deux snapshots autour du renderTime
            StateSnapshot before = null;
            StateSnapshot after = null;

            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].Timestamp <= renderTime)
                {
                    before = snapshots[i];
                }
                else
                {
                    after = snapshots[i];
                    break;
                }
            }

            // Cas: pas assez de données
            if (before == null)
            {
                // Utiliser le premier snapshot disponible
                if (snapshots.Count > 0 && snapshots[0].Players.ContainsKey(playerId))
                {
                    var p = snapshots[0].Players[playerId];
                    return new Vector2(p.x, p.y);
                }
                return null;
            }

            // Cas: on est après tous les snapshots (extrapolation légère)
            if (after == null)
            {
                if (before.Players.ContainsKey(playerId))
                {
                    var p = before.Players[playerId];
                    return new Vector2(p.x, p.y);
                }
                return null;
            }

            // Vérifier que le joueur existe dans les deux snapshots
            if (!before.Players.ContainsKey(playerId) || !after.Players.ContainsKey(playerId))
            {
                // Retourner ce qu'on a
                if (before.Players.ContainsKey(playerId))
                {
                    var p = before.Players[playerId];
                    return new Vector2(p.x, p.y);
                }
                if (after.Players.ContainsKey(playerId))
                {
                    var p = after.Players[playerId];
                    return new Vector2(p.x, p.y);
                }
                return null;
            }

            // Interpolation linéaire entre les deux positions
            float t = (renderTime - before.Timestamp) / (after.Timestamp - before.Timestamp);
            t = Mathf.Clamp01(t);

            var posBefore = new Vector2(before.Players[playerId].x, before.Players[playerId].y);
            var posAfter = new Vector2(after.Players[playerId].x, after.Players[playerId].y);

            return Vector2.Lerp(posBefore, posAfter, t);
        }

        /// <summary>
        /// Retourne l'état interpolé d'un counter
        /// </summary>
        public CounterState GetInterpolatedCounterState(string counterId, float currentTime)
        {
            float renderTime = currentTime - interpolationDelay;

            StateSnapshot before = null;
            StateSnapshot after = null;

            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].Timestamp <= renderTime)
                {
                    before = snapshots[i];
                }
                else
                {
                    after = snapshots[i];
                    break;
                }
            }

            if (before == null && snapshots.Count > 0)
            {
                before = snapshots[0];
            }

            if (before != null && before.Counters.ContainsKey(counterId))
            {
                var counterBefore = before.Counters[counterId];

                // Pour le cooking progress, on peut interpoler
                if (after != null && after.Counters.ContainsKey(counterId))
                {
                    var counterAfter = after.Counters[counterId];
                    float t = (renderTime - before.Timestamp) / (after.Timestamp - before.Timestamp);
                    t = Mathf.Clamp01(t);

                    return new CounterState
                    {
                        id = counterBefore.id,
                        type = counterBefore.type,
                        currentItem = counterAfter.currentItem, // Prendre le plus récent
                        cookingState = counterAfter.cookingState,
                        cookingProgress = Mathf.Lerp(counterBefore.cookingProgress, counterAfter.cookingProgress, t),
                        lockedBy = counterAfter.lockedBy
                    };
                }

                return counterBefore;
            }

            return null;
        }

        /// <summary>
        /// Retourne l'état complet le plus récent (pour réconciliation)
        /// </summary>
        public StateSnapshot GetLatestSnapshot()
        {
            if (snapshots.Count == 0) return null;
            return snapshots[snapshots.Count - 1];
        }

        /// <summary>
        /// Retourne le snapshot correspondant à un tick spécifique
        /// </summary>
        public StateSnapshot GetSnapshotAtTick(uint tick)
        {
            for (int i = snapshots.Count - 1; i >= 0; i--)
            {
                if (snapshots[i].Tick == tick)
                {
                    return snapshots[i];
                }
                if (snapshots[i].Tick < tick)
                {
                    break;
                }
            }
            return null;
        }

        /// <summary>
        /// Retourne l'item porté par un joueur (état interpolé)
        /// </summary>
        public string GetInterpolatedCarry(int playerId, float currentTime)
        {
            float renderTime = currentTime - interpolationDelay;

            // Pour le carry, on prend le snapshot le plus proche avant renderTime
            for (int i = snapshots.Count - 1; i >= 0; i--)
            {
                if (snapshots[i].Timestamp <= renderTime)
                {
                    if (snapshots[i].Players.ContainsKey(playerId))
                    {
                        return snapshots[i].Players[playerId].carry;
                    }
                    break;
                }
            }

            // Fallback: dernier snapshot
            if (snapshots.Count > 0)
            {
                var latest = snapshots[snapshots.Count - 1];
                if (latest.Players.ContainsKey(playerId))
                {
                    return latest.Players[playerId].carry;
                }
            }

            return null;
        }

        /// <summary>
        /// Nettoie les vieux snapshots
        /// </summary>
        public void CleanOldSnapshots(float currentTime, float maxAge = 1.0f)
        {
            float cutoffTime = currentTime - maxAge;
            snapshots.RemoveAll(s => s.Timestamp < cutoffTime);
        }

        /// <summary>
        /// Vide le buffer
        /// </summary>
        public void Clear()
        {
            snapshots.Clear();
        }

        /// <summary>
        /// Nombre de snapshots dans le buffer
        /// </summary>
        public int Count => snapshots.Count;
    }
}
