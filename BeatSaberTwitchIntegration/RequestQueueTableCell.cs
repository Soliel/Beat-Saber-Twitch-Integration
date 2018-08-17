using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using NLog;
using TwitchIntegrationPlugin.UI;

namespace TwitchIntegrationPlugin
{
    class RequestQueueTableCell : StandardLevelListTableCell
    {
        QueuedSong _song;

        protected override void Awake()
        {
            base.Awake();
        }

        public void Init(QueuedSong song)
        {
            CustomLevelListTableCell cell = GetComponent<CustomLevelListTableCell>();
             
            foreach (FieldInfo info in cell.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                info.SetValue(this, info.GetValue(cell));
            }

            Destroy(cell);

            this._song = song;

            songName = this._song.BeatName;
            author = this._song.AuthName;
            StartCoroutine(TwitchIntegrationUi.LoadSprite(this._song.CoverUrl, this));
        }
    }
}
