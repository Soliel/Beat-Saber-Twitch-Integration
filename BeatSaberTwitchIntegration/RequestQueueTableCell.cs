using System.Reflection;
using TwitchIntegrationPlugin.Serializables;
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

            _song = song;

            songName = _song.BeatName;
            author = _song.AuthName;
            StartCoroutine(TwitchIntegrationUi.LoadSprite(_song.CoverUrl, this));
        }
    }
}
