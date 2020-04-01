﻿using CADability.Attribute;
using CADability.GeoObject;

namespace CADability.UserInterface
{
    /// <summary>
    /// 
    /// </summary>

    public class LineWidthSelectionProperty : MultipleChoiceProperty
    {
        private LineWidthList lineWidthList;
        private IGeoObject toWatch;
        private ILineWidth iLineWidth;
        public LineWidthSelectionProperty(string ResourceId, LineWidthList lineWidthList, LineWidth select) : this(ResourceId, lineWidthList, select, false)
        {
        }
        public LineWidthSelectionProperty(string ResourceId, LineWidthList lineWidthList, LineWidth select, bool includeUndefined)
        {
            this.lineWidthList = lineWidthList;
            resourceId = ResourceId;
            if (includeUndefined)
            {
                choices = new string[lineWidthList.Count + 1];
                for (int i = 0; i < lineWidthList.Count; ++i)
                {
                    choices[i + 1] = lineWidthList[i].Name;
                }
                string undef = StringTable.GetString("LineWidth.Undefined");
                // sollte es den Namen schon geben, werden solange - davor und dahintergemacht, bis es den Namen mehr gibt
                while (lineWidthList.Find(undef) != null) undef = "-" + undef + "-";
                choices[0] = undef;
                if (select != null)
                {
                    base.selectedText = select.Name;
                }
                else
                {
                    base.selectedText = undef;
                }
            }
            else
            {
                choices = new string[lineWidthList.Count];
                for (int i = 0; i < lineWidthList.Count; ++i)
                {
                    choices[i] = lineWidthList[i].Name;
                }
                if (select != null)
                {
                    base.selectedText = select.Name;
                }
            }
        }
        public LineWidthSelectionProperty(string ResourceId, LineWidthList lineWidthList, ILineWidth iLineWidth, bool includeUndefined)
        {
            this.lineWidthList = lineWidthList;
            resourceId = ResourceId;
            LineWidth select = iLineWidth.LineWidth;
            if (includeUndefined)
            {
                choices = new string[lineWidthList.Count + 1];
                for (int i = 0; i < lineWidthList.Count; ++i)
                {
                    choices[i + 1] = lineWidthList[i].Name;
                }
                string undef = StringTable.GetString("LineWidth.Undefined");
                // sollte es den Namen schon geben, werden solange - davor und dahintergemacht, bis es den Namen mehr gibt
                while (lineWidthList.Find(undef) != null) undef = "-" + undef + "-";
                choices[0] = undef;
                if (select != null)
                {
                    base.selectedText = select.Name;
                }
                else
                {
                    base.selectedText = undef;
                }
            }
            else
            {
                choices = new string[lineWidthList.Count];
                for (int i = 0; i < lineWidthList.Count; ++i)
                {
                    choices[i] = lineWidthList[i].Name;
                }
                if (select != null)
                {
                    base.selectedText = select.Name;
                }
            }
            this.iLineWidth = iLineWidth;
            toWatch = iLineWidth as IGeoObject;
        }
        public delegate void LineWidthChangedDelegate(LineWidth selected);
        public event LineWidthChangedDelegate LineWidthChangedEvent;
        protected override void OnSelectionChanged(string selected)
        {
            base.OnSelectionChanged(selected);
            // und weiterleiten
            if (LineWidthChangedEvent != null) LineWidthChangedEvent(lineWidthList.Find(selected));
            if (iLineWidth != null) iLineWidth.LineWidth = lineWidthList.Find(selected);
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.Added"/>
        /// </summary>
        /// <param name="propertyTreeView"></param>
        public override void Added(IPropertyPage propertyTreeView)
        {
            base.Added(propertyTreeView);
            if (toWatch != null) toWatch.DidChangeEvent += new ChangeDelegate(GeoObjectDidChange);
        }
        /// <summary>
        /// Overrides <see cref="IShowPropertyImpl.Removed"/>
        /// </summary>
        /// <param name="propertyTreeView">the IPropertyTreeView from which it was removed</param>
        public override void Removed(IPropertyPage propertyTreeView)
        {
            base.Removed(propertyTreeView);
            if (toWatch != null) toWatch.DidChangeEvent -= new ChangeDelegate(GeoObjectDidChange);
        }
        private void GeoObjectDidChange(IGeoObject Sender, GeoObjectChange Change)
        {
            if (Sender == toWatch && Change.OnlyAttributeChanged && propertyTreeView != null)
            {
                if ((Change as GeoObjectChange).MethodOrPropertyName == "LineWidth" ||
                    (Change as GeoObjectChange).MethodOrPropertyName == "Style")
                {
                    if ((toWatch as ILineWidth).LineWidth != null) base.selectedText = (toWatch as ILineWidth).LineWidth.Name;
                    else base.selectedText = null;
                    propertyTreeView.Refresh(this);
                }
            }
        }
        public IGeoObject Connected
        {   // mit dieser Property kann man das kontrollierte Geoobjekt ändern
            get { return toWatch; }
            set
            {
                if (base.propertyTreeView != null)
                {   // dann ist diese Property schon Added und nicht removed
                    if (toWatch != null) toWatch.DidChangeEvent -= new ChangeDelegate(GeoObjectDidChange);
                }
                toWatch = value;
                iLineWidth = value as ILineWidth;
                if (toWatch != null) toWatch.DidChangeEvent += new ChangeDelegate(GeoObjectDidChange);
            }
        }
    }
}
