using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using NLog;

namespace TwitchIntegrationPlugin
{
    class RequestQueueTableCell : StandardLevelListTableCell
    {
        QueuedSong song;

        protected override void Awake()
        {
            base.Awake();
        }

        public void Init(QueuedSong _song)
        {
            CustomLevelListTableCell cell = GetComponent<CustomLevelListTableCell>();
             
            foreach (FieldInfo info in cell.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                info.SetValue(this, info.GetValue(cell));
            }

            Destroy(cell);

            song = _song;

            songName = song._beatName;
            author = song._authName;
            StartCoroutine(TwitchIntegrationUI.LoadSprite(song._coverUrl, this));
        }
    }
}
