using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalMoonUnlocks.Util {
    internal class DelayHelper : MonoBehaviour {

        public static DelayHelper Instance {  get; private set; }

        private void Awake() {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            } else {
                Destroy(gameObject);
            }
        }
        public void ExecuteAfterDelay(Action action, float delay) {
            StartCoroutine(DelayedExecution(action, delay));
        }

        private IEnumerator DelayedExecution(Action action, float delay) {
            yield return new WaitForSeconds(delay);
            action.Invoke();
        }
    }
}
