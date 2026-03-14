using System;
using System.Collections.Generic;
using UnityEngine;

namespace CookMoiCa.Network
{
    // ============================================================
    // ENUMS
    // ============================================================

    public enum NetworkRole
    {
        Host,   // Borne d'arcade - source de vérité
        Client  // Navigateur web - prediction + réconciliation
    }

    public enum ActionType
    {
        None,
        Grab,
        Drop,
        Interact
    }

    // ============================================================
    // INPUT MESSAGES (Client → Serveur)
    // ============================================================

    [Serializable]
    public class InputMessage
    {
        public string type = "input";
        public uint tick;
        public int playerId;
        public float horizontal;
        public float vertical;
        public string action; // "none", "grab", "drop", "interact"
        public string targetId; // ID du counter/item ciblé (optionnel)

        public InputMessage() { }

        public InputMessage(uint tick, int playerId, float h, float v, ActionType action, string targetId = null)
        {
            this.tick = tick;
            this.playerId = playerId;
            this.horizontal = h;
            this.vertical = v;
            this.action = action.ToString().ToLower();
            this.targetId = targetId;
        }
    }

    // ============================================================
    // STATE MESSAGES (Serveur → Clients)
    // ============================================================

    [Serializable]
    public class PlayerState
    {
        public int id;
        public float x;
        public float y;
        public string carry; // Nom de l'item porté ou null
        public bool isFrozen;
        public float moveX;
        public float moveY;
        public float lastMoveX;
        public float lastMoveY;

        public PlayerState() { }

        public PlayerState(int id, Vector2 pos, string carry, bool frozen)
        {
            this.id = id;
            this.x = pos.x;
            this.y = pos.y;
            this.carry = carry;
            this.isFrozen = frozen;
        }
    }

    [Serializable]
    public class CounterState
    {
        public string id;
        public string type;
        public string currentItem; // Nom de l'item ou null
        public string cookingState; // "idle", "cooking", "done"
        public float cookingProgress; // 0.0 à 1.0
        public float cookingDuration; // durée totale en secondes (pour simulation locale côté client)
        public int lockedBy = -1; // playerId qui utilise ce counter (-1 = aucun)

        public CounterState() { }
    }

    [Serializable]
    public class OrderState
    {
        public string id;
        public string recipeName;
        public float timeRemaining;
        public string status; // "pending", "completed", "expired"

        public OrderState() { }
    }

    [Serializable]
    public class PNJState
    {
        public string id;
        public float x;
        public float y;
        public string etat; // "arrive", "attendCommande", "attendService", "satisfait", "insatisfait"
        public string recipeName; // Nom de la recette commandée (si en attente de service)
        public float lastMoveX;
        public float lastMoveY;
        public bool isMoving;

        public PNJState() { }
    }

    [Serializable]
    public class FullStateMessage
    {
        public string type = "fullState";
        public uint tick;
        public float timeLeft;
        public int score;
        public string gameState; // "waiting", "ready", "playing", "gameover"
        public List<PlayerState> players = new List<PlayerState>();
        public List<CounterState> counters = new List<CounterState>();
        public List<OrderState> orders = new List<OrderState>();
        public List<PNJState> pnjs = new List<PNJState>();

        public FullStateMessage() { }
    }

    [Serializable]
    public class DeltaStateMessage
    {
        public string type = "delta";
        public uint tick;
        public float timeLeft;
        public bool hasTimeLeft;
        public int score;
        public bool hasScore;
        public List<PlayerState> players; // Seulement ceux qui ont changé
        public List<CounterState> counters; // Seulement ceux qui ont changé
        public List<OrderState> orders; // Commandes actives
        public List<PNJState> pnjs; // PNJ actifs

        public DeltaStateMessage() { }
    }

    // ============================================================
    // EVENT MESSAGES (Serveur → Clients) - Instantané
    // ============================================================

    [Serializable]
    public class GameEventMessage
    {
        public string type = "event";
        public uint tick;
        public string eventName;
        public string data; // JSON sérialisé des données spécifiques

        public GameEventMessage() { }

        public GameEventMessage(uint tick, string eventName, object eventData)
        {
            this.tick = tick;
            this.eventName = eventName;
            this.data = JsonUtility.ToJson(eventData);
        }
    }

    // Event data classes
    [Serializable]
    public class ItemGrabbedEvent
    {
        public int playerId;
        public string itemId;
        public string counterId;
    }

    [Serializable]
    public class ItemDroppedEvent
    {
        public int playerId;
        public string itemId;
        public string counterId;
    }

    [Serializable]
    public class CookingStartedEvent
    {
        public string counterId;
        public string itemName;
        public float duration;
    }

    [Serializable]
    public class CookingCompletedEvent
    {
        public string counterId;
        public string resultItem;
    }

    [Serializable]
    public class OrderCompletedEvent
    {
        public string orderId;
        public int playerId;
        public int scoreGained;
    }

    [Serializable]
    public class ScoreUpdatedEvent
    {
        public int points;
        public int total;
    }

    // PNJ lifecycle events (Host → Client)
    [Serializable]
    public class PNJSpawnEvent
    {
        public string networkId;
        public int positionIndex;
    }

    [Serializable]
    public class PNJOrderEvent
    {
        public string networkId;
        public string recipeName;
    }

    [Serializable]
    public class PNJServedEvent
    {
        public string networkId;
    }

    [Serializable]
    public class PNJExpiredEvent
    {
        public string networkId;
    }

    [Serializable]
    public class EffectActivatedEvent
    {
        public string effectType; // "SolGlissant", "ObjetsCollants", "SprintBoost", "MultiplicateurPoints", "LivraisonInstantanee"
        public float duration;
    }

    // ============================================================
    // LOBBY MESSAGES
    // ============================================================

    [Serializable]
    public class RegisterHostMessage
    {
        public string type = "registerAsHost";
    }

    [Serializable]
    public class SetupLobbyMessage
    {
        public string type = "setupLobby";
    }

    [Serializable]
    public class StartGameMessage
    {
        public string type = "startGame";
        public int localPlayerCount = 1;
    }

    [Serializable]
    public class EndGameMessage
    {
        public string type = "endGame";
        public int score;
    }

    [Serializable]
    public class JoinLobbyMessage
    {
        public string type = "joinLobby";
        public string name;
    }

    [Serializable]
    public class PlayerJoinedMessage
    {
        public int slot;
        public string name;
    }

    [Serializable]
    public class RegisterAsPlayerMessage
    {
        public string type = "registerAsPlayer";
        public int slot;
        public string name;
    }

    [Serializable]
    public class PlayerReadyMessage
    {
        public string type = "playerReady";
        public int slot;
    }

    [Serializable]
    public class PlayerLeftMessage
    {
        public int slot;
    }

    // ============================================================
    // SNAPSHOT pour Interpolation/Prediction
    // ============================================================

    public class StateSnapshot
    {
        public uint Tick { get; set; }
        public float Timestamp { get; set; }
        public Dictionary<int, PlayerState> Players { get; set; } = new Dictionary<int, PlayerState>();
        public Dictionary<string, CounterState> Counters { get; set; } = new Dictionary<string, CounterState>();
        public Dictionary<string, OrderState> Orders { get; set; } = new Dictionary<string, OrderState>();
        public Dictionary<string, PNJState> PNJs { get; set; } = new Dictionary<string, PNJState>();
        public float TimeLeft { get; set; }
        public int Score { get; set; }

        public StateSnapshot() { }

        public StateSnapshot(uint tick, float timestamp)
        {
            Tick = tick;
            Timestamp = timestamp;
        }

        public StateSnapshot Clone()
        {
            var clone = new StateSnapshot
            {
                Tick = this.Tick,
                Timestamp = this.Timestamp,
                TimeLeft = this.TimeLeft,
                Score = this.Score
            };

            foreach (var kvp in Players)
            {
                clone.Players[kvp.Key] = new PlayerState
                {
                    id = kvp.Value.id,
                    x = kvp.Value.x,
                    y = kvp.Value.y,
                    carry = kvp.Value.carry,
                    isFrozen = kvp.Value.isFrozen,
                    moveX = kvp.Value.moveX,
                    moveY = kvp.Value.moveY,
                    lastMoveX = kvp.Value.lastMoveX,
                    lastMoveY = kvp.Value.lastMoveY
                };
            }

            foreach (var kvp in Counters)
            {
                clone.Counters[kvp.Key] = new CounterState
                {
                    id = kvp.Value.id,
                    type = kvp.Value.type,
                    currentItem = kvp.Value.currentItem,
                    cookingState = kvp.Value.cookingState,
                    cookingProgress = kvp.Value.cookingProgress,
                    lockedBy = kvp.Value.lockedBy
                };
            }

            foreach (var kvp in Orders)
            {
                clone.Orders[kvp.Key] = new OrderState
                {
                    id = kvp.Value.id,
                    recipeName = kvp.Value.recipeName,
                    timeRemaining = kvp.Value.timeRemaining,
                    status = kvp.Value.status
                };
            }

            foreach (var kvp in PNJs)
            {
                clone.PNJs[kvp.Key] = new PNJState
                {
                    id = kvp.Value.id,
                    x = kvp.Value.x,
                    y = kvp.Value.y,
                    etat = kvp.Value.etat,
                    recipeName = kvp.Value.recipeName,
                    lastMoveX = kvp.Value.lastMoveX,
                    lastMoveY = kvp.Value.lastMoveY,
                    isMoving = kvp.Value.isMoving
                };
            }

            return clone;
        }
    }

    public class InputSnapshot
    {
        public uint Tick { get; set; }
        public int PlayerId { get; set; }
        public Vector2 Movement { get; set; }
        public ActionType Action { get; set; }
        public string TargetId { get; set; }

        public InputSnapshot() { }

        public InputSnapshot(uint tick, int playerId, Vector2 movement, ActionType action, string targetId = null)
        {
            Tick = tick;
            PlayerId = playerId;
            Movement = movement;
            Action = action;
            TargetId = targetId;
        }
    }
}
