using System;
using Grasshopper.Kernel;

namespace MiliumRhino5.GH_MIDI.Components
{
    /// <summary>
    ///     Provides a base class for a custom component with a button. This button will invoke an event.
    /// </summary>
    public abstract class ButtonComponent : GH_Component
    {
        public string ButtonName;

        protected ButtonComponent(
            string name,
            string nickname,
            string description,
            string category,
            string subCategory,
            string buttonName)
            : base(name, nickname, description, category, subCategory)
        {
            ButtonName = buttonName;
            DelayedPostConstructor();
        }

        protected override void PostConstructor()
        {
        }

        /// <summary>
        ///     The default PostConstructor will be delayed so the buttonName can be passed to the base constructor which creates
        ///     the interface based on the component attributes.
        ///     Otherwise the attributes are set before the constructor sets the buttonName.
        /// </summary>
        protected void DelayedPostConstructor()
        {
            base.PostConstructor();
        }

        public override void CreateAttributes()
        {
            Attributes = new ButtonComponentAttributes(this, ButtonName);
            ((ButtonComponentAttributes) Attributes).MouseLeftButtonReleasedInsideRectangleEvent +=
                OnMouseLeftButtonReleasedInsideRectangle;
            ;
        }

        private void OnMouseLeftButtonReleasedInsideRectangle(object sender, EventArgs e)
        {
            ButtonClickEvent.Invoke(this, new EventArgs());
        }

        public event EventHandler ButtonClickEvent;
    }
}