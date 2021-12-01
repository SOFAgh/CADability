using CADability.GeoObject;

namespace CADability.Attribute
{
    /// <summary>
    /// ApplicationException class raised by various operations with attributes.
    /// </summary>

    public class AttributeException : System.ApplicationException
    {
        /// <summary>
        /// Type of exception
        /// </summary>
        public enum AttributeExceptionType
        {
            /// <summary>
            /// Unspecific
            /// </summary>
            General,
            /// <summary>
            /// Invalid Argument
            /// </summary>
            InvalidArg
        };
        /// <summary>
        /// Type of exception
        /// </summary>
        public AttributeExceptionType ExceptionType;
        internal AttributeException(string message, AttributeExceptionType tp) : base(message)
        {
            ExceptionType = tp;
        }
    }
    /// <summary>
    /// Interface implemented by <see cref="Project"/> and <see cref="Settings"/>.
    /// Helps to get all kind of attribute lists, e.g. <see cref="ColorList"/>, <see cref="LayerList"/>
    /// </summary>

    public interface IAttributeListContainer
    {
        ColorList ColorList { get; }
        LayerList LayerList { get; }
        HatchStyleList HatchStyleList { get; }
        DimensionStyleList DimensionStyleList { get; }
        LineWidthList LineWidthList { get; }
        LinePatternList LinePatternList { get; }
        StyleList StyleList { get; }
        IAttributeList GetList(string KeyName);
        IAttributeList List(int KeyIndex);
        string ListKeyName(int KeyIndex);
        int ListCount { get; }
        void Add(string KeyName, IAttributeList ToAdd);
        void Remove(string KeyName);
        void AttributeChanged(IAttributeList list, INamedAttribute attribute, ReversibleChange change);
        bool RemovingItem(IAttributeList list, INamedAttribute attribute, string resourceId);
        void UpdateList(IAttributeList list);
    }


    /// <summary>
    /// static functions to manage IAttributeListContainer and IAttributeList objects
    /// </summary>

    public class AttributeListContainer
    {
        public AttributeListContainer()
        {
            // 
            // TODO: Add constructor logic here
            //
        }
        static public bool CloneAttributeList(IAttributeListContainer From, IAttributeListContainer To, string ListName, bool Initialize)
        {
            IAttributeList toAdd = null;
            if (From != null)
            {
                toAdd = From.GetList(ListName);
                if (toAdd != null)
                {
                    toAdd = toAdd.Clone();
                }
            }
            if (toAdd == null)
            {
                switch (ListName)
                {
                    case "ColorList":
                        toAdd = new ColorList();
                        break;
                    case "LayerList":
                        toAdd = new LayerList();
                        break;
                    case "HatchStyleList":
                        toAdd = new HatchStyleList();
                        break;
                    case "DimensionStyleList":
                        toAdd = new DimensionStyleList();
                        break;
                    case "LineWidthList":
                        toAdd = new LineWidthList();
                        break;
                    case "LinePatternList":
                        toAdd = new LinePatternList();
                        break;
                    case "StyleList":
                        toAdd = new StyleList();
                        break;
                    default:
                        return false;
                }
                if (Initialize)
                    toAdd.Initialize();
            }
            To.Add(ListName, toAdd);
            return true;
        }

        static public void UpdateLists(IAttributeListContainer container, bool AddMissingToList)
        {
            if (container == null) return;
            for (int i = 0; i < container.ListCount; i++)
                container.List(i).Update(AddMissingToList); ;
        }

        static public bool IncludeAttributeList(IAttributeListContainer Including, IAttributeListContainer Included, string ListName, bool Initialize)
        {
            bool res = false;
            if (Including == null || Included == null) return res;
            IAttributeList toAdd = Included.GetList(ListName);
            IAttributeList addTo = Including.GetList(ListName);
            if (toAdd == null || addTo == null) return res;
            for (int i = 0; i < toAdd.Count; i++)
            {
                INamedAttribute na = toAdd.Item(i);
                if (addTo.Find(na.Name) == null)
                {
                    addTo.Add(na);
                    res = true;
                }
            }
            if (res)
                UpdateLists(Including, false);
            return res;
        }
        static public void UpdateObjectAttrinutes(IAttributeListContainer container, IGeoObject Object2Update)
        {
            if (container == null) return;
            for (int i = 0; i < container.ListCount; i++)
            {
                container.List(i).Update(Object2Update);
                if (Object2Update.HasChildren())
                    for (int j = 0; j < Object2Update.NumChildren; j++)
                        container.List(i).Update(Object2Update.Child(j));

            }
        }


    }
}
