using HMUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NLog;
using TMPro;
using UnityEngine;
using VRUI;

namespace TwitchIntegrationPlugin
{
    class RequestQueueController : VRUIViewController, TableView.IDataSource
    {
        TextMeshProUGUI _titleText;
        TwitchIntegrationUI Ui;

        TableView _queuedSongsTableView;
        StandardLevelListTableCell _songListTableCellInstance;

        List<QueuedSong> _top5Queued;
        List<QueuedSong> _queuedSongs;
        NLog.Logger logger;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            logger = LogManager.GetCurrentClassLogger();
            logger.Debug("QueueController Activate.");
            Ui = TwitchIntegrationUI._instance;

            _songListTableCellInstance = Resources.FindObjectsOfTypeAll<StandardLevelListTableCell>().First(x => (x.name == "SongListTableCell"));
            _queuedSongs = StaticData.queueList.OfType<QueuedSong>().ToList();
            _top5Queued = (List<QueuedSong>)_queuedSongs.Take(5);

            if (_titleText == null)
            {
                _titleText = Ui.CreateText(rectTransform, "REQUEST QUEUE", new Vector2(0f, -6f));
                _titleText.alignment = TextAlignmentOptions.Top;
                _titleText.fontSize = 8;
            }

            if (_queuedSongsTableView == null)
            {
                _queuedSongsTableView = new GameObject().AddComponent<TableView>();

                _queuedSongsTableView.transform.SetParent(rectTransform, false);

                _queuedSongsTableView.dataSource = this;

                (_queuedSongsTableView.transform as RectTransform).anchorMin = new Vector2(0.3f, 0.5f);
                (_queuedSongsTableView.transform as RectTransform).anchorMax = new Vector2(0.7f, 0.5f);
                (_queuedSongsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
                (_queuedSongsTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, -3f);

                _queuedSongsTableView.didSelectRowEvent += _queuedSongsTableView_DidSelectRowEvent;
            }
        }

        private void _queuedSongsTableView_DidSelectRowEvent(TableView arg1, int arg2)
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
            return _queuedSongs.Count();
        }

        public TableCell CellForRow(int row)
        {
            StandardLevelListTableCell _tableCell = Instantiate(_songListTableCellInstance);

            RequestQueueTableCell _queueCell = _tableCell.gameObject.AddComponent<RequestQueueTableCell>();

            _queueCell.Init(_queuedSongs[row]);

            return _queueCell;
        }
    }
}
