using System;
using System.Drawing;
using System.Reflection;

namespace CADability.UserInterface
{
    /// <summary>
    /// 
    /// </summary>

    public class NameProperty : IShowPropertyImpl
    {
        private object ObjectWithName;
        private PropertyInfo TheProperty;
        private bool IsSetting;

        public NameProperty(object ObjectWithName, string PropertyName, string resourceId)
        {
            IsSetting = false;
            this.ObjectWithName = ObjectWithName;
            base.resourceId = resourceId;
            TheProperty = ObjectWithName.GetType().GetProperty(PropertyName);
            if (TheProperty == null) TheProperty = ObjectWithName.GetType().BaseType.GetProperty(PropertyName); // besser rekursiv
        }
        
        private void SetName(string s)
        {
            MethodInfo mi = TheProperty.GetSetMethod();
            object[] prm = new object[1];
            prm[0] = s;
            try
            {
                IsSetting = true;
                mi.Invoke(ObjectWithName, prm);
            }
            finally
            {
                IsSetting = false;
            }
        }
        private string GetName()
        {
            MethodInfo mi = TheProperty.GetGetMethod();
            object[] prm = new Object[0];
            return (string)mi.Invoke(ObjectWithName, prm);
        }

        #region IShowPropertyImpl Overrides


        /// <summary>
        /// Overrides <see cref="CADability.UserInterface.IShowPropertyImpl.UnSelected ()"/>
        /// </summary>
        public override void UnSelected()
        {
            base.UnSelected();
        }
        #endregion
        public override string Value
        {
            get
            {
                string res = GetName();
                if (res == null) return ""; // if null is returned, no edit field will be displayed
                else return res;
            }
        }
        private string oldName;
        public override void StartEdit(bool editValue)
        {
            oldName = GetName();
        }
        public override bool EditTextChanged(string newValue)
        {
            using (Frame.Project.Undo.ContextFrame(this))
            {
                SetName(newValue);
                return true;
            }
        }
        public override void EndEdit(bool aborted, bool modified, string newValue)
        {
            if (aborted)
            {
                SetName(oldName);
            }
            else if (modified)
            {
                SetName(newValue);
            }
            Frame.Project.Undo.ClearContext();
        }
    }
}
