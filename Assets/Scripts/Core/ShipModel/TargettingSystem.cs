using System.Collections.Generic;
using Core.Player;
using Core.Replays;
using Game_UI;
using Misc;
using UnityEngine;

namespace Core.ShipModel {
    public class TargettingSystem : MonoBehaviour {
        [SerializeField] private Target targetPrefab;
        private readonly Dictionary<ShipGhost, Target> _ghosts = new();
        private readonly Dictionary<ShipPlayer, Target> _players = new();
        private Camera _mainCamera;

        private void Update() {
            // update camera if needed
            if (_mainCamera == null || _mainCamera.enabled == false || _mainCamera.gameObject.activeSelf == false)
                _mainCamera = Camera.main;


            // update target objects for players
            foreach (var keyValuePair in _players) {
                var player = keyValuePair.Key;
                if (player == null) continue;
                var target = keyValuePair.Value;
                var targetName = player.playerName;
                var targetPosition = player.User.transform.position;
                UpdateTarget(target, targetPosition, targetName);
            }

            foreach (var keyValuePair in _ghosts) {
                var ghost = keyValuePair.Key;
                if (ghost == null) continue;
                var target = keyValuePair.Value;
                var targetName = ghost.PlayerName;
                var targetPosition = ghost.transform.position;
                UpdateTarget(target, targetPosition, targetName);
            }
        }

        private void OnEnable() {
            Game.OnVRStatus += OnVRStatusChanged;
            Game.OnPlayerLoaded += ResetTargets;
            Game.OnPlayerLeave += ResetTargets;
            Game.OnGhostAdded += ResetTargets;
            Game.OnGhostRemoved += ResetTargets;
            _mainCamera = Camera.main;
        }

        private void OnDisable() {
            Game.OnVRStatus -= OnVRStatusChanged;
            Game.OnPlayerLoaded -= ResetTargets;
            Game.OnPlayerLeave -= ResetTargets;
            Game.OnGhostAdded -= ResetTargets;
            Game.OnGhostRemoved -= ResetTargets;
        }

        private void UpdateTarget(Target target, Vector3 targetPosition, string targetName) {
            var player = FdPlayer.FindLocalShipPlayer;
            var originPosition = player ? player.User.UserCameraPosition : Vector3.zero;
            var shipPosition = player ? player.transform.position : Vector3.zero;

            var distance = Vector3.Distance(originPosition, targetPosition);
            var distanceToShip = Vector3.Distance(shipPosition, targetPosition);
            var direction = (targetPosition - originPosition).normalized;

            target.Name = targetName;
            target.DistanceMeters = distance;

            var minDistance = 10f;
            var maxDistance = 30f + minDistance;

            var targetTransform = target.transform;
            targetTransform.position = Vector3.MoveTowards(originPosition, targetPosition + direction * minDistance, maxDistance);
            targetTransform.rotation = _mainCamera.transform.rotation;

            target.Opacity = MathfExtensions.Remap(5, minDistance, 0, 1, distanceToShip);
        }

        private void ResetTargets() {
            foreach (var keyValuePair in _players) Destroy(keyValuePair.Value.gameObject);
            foreach (var keyValuePair in _ghosts) Destroy(keyValuePair.Value.gameObject);
            _players.Clear();
            _ghosts.Clear();

            foreach (var shipPlayer in FdNetworkManager.Instance.ShipPlayers)
                if (!shipPlayer.isLocalPlayer) {
                    var target = Instantiate(targetPrefab, transform);
                    _players.Add(shipPlayer, target);
                }

            foreach (var replayShip in FindObjectsOfType<ShipGhost>()) {
                var target = Instantiate(targetPrefab, transform);
                _ghosts.Add(replayShip, target);
            }
        }

        private void OnVRStatusChanged(bool vrEnabled) {
            // rebuild targets to reset all rotations
            if (!vrEnabled) ResetTargets();
        }
    }
}