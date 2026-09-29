namespace Timeline
{
    /// <summary>
    /// What an edit calls to say it changed something. The window notices most changes by itself, so
    /// these only make sure it looks again this frame, and keep the toolbar button's state current.
    /// </summary>
    public partial class Timeline
    {
        private void UpdateInterpolablesView()
        {
            _toolbarButton?.UpdateButton();
            if (_view != null)
                _view.Touch();
        }

        private void UpdateGrid()
        {
            RebuildSelectedKeyframeSet();
            if (_view != null)
                _view.Touch();
        }

        private void UpdateStrips()
        {
            if (_view != null)
                _view.Touch();
        }

        private void UpdateMarkers()
        {
            if (_view != null)
                _view.Touch();
        }

        private void UpdateKeyframeWindow(bool changeShowState = true)
        {
            RebuildSelectedKeyframeSet();
            if (_view != null)
                _view.Touch();
        }
    }
}
