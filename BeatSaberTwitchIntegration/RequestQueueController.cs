using HMUI;
using System.Collections.Generic;
using System.Linq;
using NLog;
using TMPro;
using TwitchIntegrationPlugin.UI;
using UnityEngine;
using VRUI;

namespace TwitchIntegrationPlugin
{
    class RequestQueueController : VRUIViewController, TableView.IDataSource
    {
        private TextMeshProUGUI _titleText;
        private TwitchIntegrationUi _ui;

        private TableView _queuedSongsTableView;
        private StandardLevelListTableCell _songListTableCellInstance;

        private List<QueuedSong> _top5Queued;
        private List<QueuedSong> _queuedSongs;
        private NLog.Logger _logger;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            _logger = LogManager.GetCurrentClassLogger();
            _logger.Debug("QueueController Activate.");
            _ui = TwitchIntegrationUi.Instance;

            _songListTableCellInstance = Resources.FindObjectsOfTypeAll<StandardLevelListTableCell>().First(x => (x.name == "SongListTableCell"));
            _queuedSongs = StaticData.QueueList.OfType<QueuedSong>().ToList();
            _top5Queued = (List<QueuedSong>)_queuedSongs.Take(5);

            if (_titleText == null)
            {
                _titleText = _ui.CreateText(rectTransform, "REQUEST QUEUE", new Vector2(0f, -6f));
                _titleText.alignment = TextAlignmentOptions.Top;
                _titleText.fontSize = 8;
            }

            if (_queuedSongsTableView == null)
            {
                _queuedSongsTableView = new GameObject().AddComponent<TableView>();

                _queuedSongsTableView.transform.SetParent(rectTransform, false);

                _queuedSongsTableView.dataSource = this;

                ((RectTransform) _queuedSongsTableView.transform).anchorMin = new Vector2(0.3f, 0.5f);
                ((RectTransform) _queuedSongsTableView.transform).anchorMax = new Vector2(0.7f, 0.5f);
                ((RectTransform) _queuedSongsTableView.transform).sizeDelta = new Vector2(0f, 60f);
                ((RectTransform) _queuedSongsTableView.transform).anchoredPosition = new Vector3(0f, -3f);

                _queuedSongsTableView.didSelectRowEvent += _queuedSongsTableView_DidSelectRowEvent;
            }
        }

        private static void _queuedSongsTableView_DidSelectRowEvent(TableView arg1, int arg2)
        {

        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {

        }

        public void Dequeue()
        {
            _queuedSongs.RemoveAt(0);
            _queuedSongs.Add(_queuedSongs.Skip(4).First());
            _queuedSongsTableView.ReloadData();
        }

        public float RowHeight()
        {
            return 10f;
        }

        public int NumberOfRows()
        {
            return _queuedSongs.Count;
        }

        public TableCell CellForRow(int row)
        {
            var tableCell = Instantiate(_songListTableCellInstance);

            var queueCell = tableCell.gameObject.AddComponent<RequestQueueTableCell>();

            queueCell.Init(_queuedSongs[row]);

            return queueCell;
        }
    }
}
