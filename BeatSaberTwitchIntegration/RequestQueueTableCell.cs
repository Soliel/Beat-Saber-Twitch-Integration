using System.Reflection;
using TwitchIntegrationPlugin.UI;

namespace TwitchIntegrationPlugin
{
    public class RequestQueueTableCell : StandardLevelListTableCell
    {
        private QueuedSong _song;

        protected override void Awake()
        {
            base.Awake();
        }

        public void Init(QueuedSong song)
        {
            var cell = GetComponent<StandardLevelListTableCell>();
             
            foreach (var info in cell.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
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
