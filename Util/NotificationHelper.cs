using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LethalMoonUnlocks.Util {
    internal static class NotificationHelper {

        private static readonly List<Notification> Queue = new List<Notification>();
        private static bool IsSending = false;

        internal static void AddNotificationToQueue(Notification notification) {
            Queue.Add(notification);
            Plugin.Instance.Mls.LogDebug($"Queued new alert: header = {notification.Header}, body = {notification.Text}, warning = {notification.IsWarning}, useSave = {notification.UseSave}, key = {notification.Key}");
        }

        internal static IEnumerator SendQueuedNotifications() {
            if (IsSending) {
                Plugin.Instance.Mls.LogDebug("Trying to start queue but it's already sending..");
                yield break;
            }
            if (!ConfigManager.ShowAlerts) Queue.Clear();
            while (Queue.Count > 0) {
                IsSending = true;
                var notification = Queue.First();                    
                Plugin.Instance.Mls.LogDebug($"Sending out alert: header = {notification.Header}, body = {notification.Text}, warning = {notification.IsWarning}, useSave = {notification.UseSave}, key = {notification.Key}");
                HUDManager.Instance.DisplayTip(notification.Header, notification.Text, notification.IsWarning, notification.UseSave, notification.Key);
                Queue.Remove(notification);
                yield return new WaitForSeconds(8f);
            }
            IsSending = false;
        }

        internal static void SendChatMessage(string message) {
            if (ConfigManager.ChatMessages) {
                HUDManager.Instance.AddTextToChatOnServer(message);
            }
        }

        internal static string CountToText(this int count) {
            if (count <= 0) return string.Empty;
            if (count > 0) {
                int count_mod = count % 10;
                if (count_mod % 10 == 1) return count + "st";
                else if (count_mod == 2) return count + "nd";
                else if (count_mod == 3) return count + "rd";
                else return count + "th";
            }
            return string.Empty;
        }
        internal static string NumberOfWords(this int number, string word) {
            if (number <= 0) return string.Empty;
            if (number == 1) return "one " + word;
            if (number == 2) return "two " + word + "s";
            if (number == 3) return "three " + word + "s";
            if (number > 3) return number + " " + word + "s";

            return string.Empty;
        }

        internal static string SinglePluralWord(this int number, string word) {
            if (word.EndsWith('y')) {
                if (number > 1) {
                    return word.Substring(0, word.Length - 1) + "ies";
                } else {
                    return word;
                }
            }
            if (number > 1) return word + "s";
            else return word;
        }
    }
}
