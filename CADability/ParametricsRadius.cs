using CADability.Actions;
using CADability.GeoObject;
using CADability.UserInterface;
using System;
using System.Collections.Generic;
using System.Text;

namespace CADability
{
    class ParametricsRadius : ConstructAction
    {
        private Face[] facesWithRadius;
        private IFrame frame;
        private bool diameter;
        private Shell shell;
        private LengthInput radiusInput;
        private LengthInput diameterInput;
        private bool validResult;
        private bool useRadius;
        private bool isFillet;
        private string parametricsName;

        public ParametricsRadius(Face[] facesWithRadius, IFrame frame, bool useRadius)
        {
            this.facesWithRadius = facesWithRadius;
            this.frame = frame;
            this.useRadius = useRadius;
            shell = facesWithRadius[0].Owner as Shell;
            // Prametrics class has a problem with subdevided edges, which are connected in CombineConnectedFaces via CombineConnectedSameSurfaceEdges
            // we call this here, because during the action there is a problem with changing a GeoObject of the model and continuous changes.
            if (!shell.State.HasFlag(Shell.ShellFlags.FacesCombined)) shell.CombineConnectedFaces();
            isFillet = facesWithRadius[0].IsFillet();
        }

        public override string GetID()
        {
            return "MenuId.Parametrics.Cylinder";
        }

        public override bool OnCommand(string MenuId)
        {
            switch (MenuId)
            {
                case "MenuId.Parametrics.Cylinder.Radius":
                    diameter = false;
                    frame.SetAction(this); // this is the way this action comes to life
                    return true;
                case "MenuId.Parametrics.Cylinder.Diameter":
                    diameter = true;
                    frame.SetAction(this); // this is the way this action comes to life
                    return true;
            }
            return false;
        }

        public override void OnSelected(MenuWithHandler selectedMenuItem, bool selected)
        {

        }

        public override bool OnUpdateCommand(string MenuId, CommandState CommandState)
        {
            switch (MenuId)
            {
                case "MenuId.Parametrics.Cylinder.Radius":
                    return true;
                case "MenuId.Parametrics.Cylinder.Diameter":
                    return true;
            }
            return false;
        }

        public override void OnSetAction()
        {
            base.TitleId = "Constr.Parametrics.Cylinder.Radius";
            base.ActiveObject = shell.Clone();
            List<InputObject> actionInputs = new List<InputObject>();

            if (useRadius)
            {
                radiusInput = new LengthInput("Parametrics.Cylinder.Radius");
                radiusInput.GetLengthEvent += RadiusInput_GetLength;
                radiusInput.SetLengthEvent += RadiusInput_SetLength;
                radiusInput.Optional = diameter;
                actionInputs.Add(radiusInput);
            }
            else
            {
                diameterInput = new LengthInput("Parametrics.Cylinder.Diameter");
                diameterInput.GetLengthEvent += DiameterInput_GetLength;
                diameterInput.SetLengthEvent += DiameterInput_SetLength;
                diameterInput.Optional = !diameter;
                actionInputs.Add(diameterInput);
            }

            SeparatorInput separator = new SeparatorInput("Parametrics.Cylinder.AssociateParametric");
            base.SetInput(separator);
            StringInput nameInput = new StringInput("Parametrics.Cylinder.ParametricsName");
            nameInput.SetStringEvent += NameInput_SetStringEvent;
            nameInput.GetStringEvent += NameInput_GetStringEvent;
            nameInput.Optional = true;
            actionInputs.Add(nameInput);

            SetInput(actionInputs.ToArray());
            base.OnSetAction();

            validResult = false;
        }
        private string NameInput_GetStringEvent()
        {
            if (parametricsName == null) return string.Empty;
            else return parametricsName;
        }

        private void NameInput_SetStringEvent(string val)
        {
            parametricsName = val;
        }
        public override void OnActivate(Actions.Action OldActiveAction, bool SettingAction)
        {
            if (shell.Layer != null) shell.Layer.Transparency = 128;
            base.OnActivate(OldActiveAction, SettingAction);
        }
        public override void OnInactivate(Actions.Action NewActiveAction, bool RemovingAction)
        {
            if (shell.Layer != null) shell.Layer.Transparency = 0;
            base.OnInactivate(NewActiveAction, RemovingAction);
        }
        private bool RadiusInput_SetLength(double length)
        {
            validResult = false;
            if (shell != null && length > 0.0)
            {
                Parametric pm = new Parametric(shell);
                bool ok = false;
                if (isFillet) ok = pm.ModifyFilletRadius(facesWithRadius, length);
                else ok = pm.ModifyRadius(facesWithRadius, length);
                if (ok)
                {
                    if (pm.Apply())
                    {
                        Shell sh = pm.Result();
                        ActiveObject = sh;
                        validResult = true;
                        return true;
                    }
                    else
                    {
                        ActiveObject = shell.Clone();
                        return false;
                    }
                }
            }
            return false;
        }

        private double RadiusInput_GetLength()
        {
            if (facesWithRadius[0].Surface is ICylinder cyl) return cyl.Radius; // we only have round cylinders here
            else return 0.0;
        }
        private bool DiameterInput_SetLength(double length)
        {
            return RadiusInput_SetLength(length / 2.0);
        }

        private double DiameterInput_GetLength()
        {
            return 2.0 * RadiusInput_GetLength();
        }

        public override void OnDone()
        {
            if (validResult && ActiveObject != null)
            {
                using (Frame.Project.Undo.UndoFrame)
                {
                    Solid sld = shell.Owner as Solid;
                    if (sld != null)
                    {   // the shell was part of a Solid
                        IGeoObjectOwner owner = sld.Owner; // Model or Block
                        owner.Remove(sld);
                        Solid replacement = Solid.MakeSolid(ActiveObject as Shell);
                        owner.Add(replacement);
                    }
                    else
                    {
                        IGeoObjectOwner owner = shell.Owner;
                        owner.Remove(shell);
                        owner.Add(ActiveObject);
                    }
                }
            }
            ActiveObject = null;
            base.OnDone();
        }
    }
}
