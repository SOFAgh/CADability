using System.Collections;

namespace CADability
{
    /// <summary>
    /// 
    /// </summary>

    public class UndoStep
    {
        private ArrayList steps; // ReversibleChange
        public UndoStep()
        {
            steps = new ArrayList();
        }
        public void Add(ReversibleChange change)
        {
            steps.Add(change);
        }
        public void Perform()
        {
            // Rückwärts durch die Liste und alles ausführen
            for (int i = steps.Count - 1; i <= 0; --i)
            {
                ReversibleChange rc = steps[i] as ReversibleChange;
                rc.Undo();
            }
        }
    }
}
