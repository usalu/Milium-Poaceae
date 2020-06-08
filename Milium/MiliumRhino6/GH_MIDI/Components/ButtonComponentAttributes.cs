using System;
using System.Drawing;
using System.Windows.Forms;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;

namespace MiliumRhino6.GH_MIDI.Components
{
    /// <summary>
    ///     Provides the modified component attributes with a custom button in the interface.
    ///     A button event is triggered when the left mouse button is released after the left mouse button was clicked inside
    ///     the button area.
    /// </summary>
    internal class ButtonComponentAttributes : GH_ComponentAttributes
    {
        private readonly string _buttonName;
        private RectangleF _buttonRectangleF;
        private bool _mouseClickInside;
        private bool _mouseHoverInside;

        public ButtonComponentAttributes(ButtonComponent component, string buttonName) : base(component)
        {
            _buttonName = buttonName;
        }

        protected override void Layout()
        {
            base.Layout();
            _buttonRectangleF = new RectangleF(Bounds.Left, Bounds.Top - 15f, Bounds.Width, 15f);
            Bounds = RectangleF.Union(Bounds, _buttonRectangleF);
        }

        /// <summary>
        ///     Render the component each time after the canvas got invalid.
        ///     The custom button will be rendered according the mouse position and events.
        /// </summary>
        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);
            if (channel != GH_CanvasChannel.Objects)
                return;
            var impliedStyle =
                GH_CapsuleRenderEngine.GetImpliedStyle(GH_Palette.Transparent, Selected, Owner.Locked, true);
            var textCapsule = GH_Capsule.CreateTextCapsule(_buttonRectangleF, _buttonRectangleF, GH_Palette.Normal,
                _buttonName, GH_FontServer.Small, 1, 9);
            if (_mouseHoverInside)
                textCapsule.RenderEngine.RenderBackground_Alternative(graphics, Color.FromArgb(100, Color.DarkOrange),
                    false);
            else
                textCapsule.RenderEngine.RenderBackground_Alternative(graphics, Color.FromArgb(100, Color.DarkCyan),
                    false);
            if (_mouseClickInside)
                textCapsule.RenderEngine.RenderHighlight(graphics);
            textCapsule.RenderEngine.RenderOutlines(graphics, canvas.Viewport.Zoom, impliedStyle);
            textCapsule.RenderEngine.RenderText(graphics, Color.DarkSlateGray);
            textCapsule.Dispose();
        }

        /// <summary>
        ///     This method subscribes to mouse down event in the document.
        ///     The background color of the button will be set to a special color to indicate that the button was clicked inside
        ///     the render function.
        /// </summary>
        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (!_buttonRectangleF.Contains(e.CanvasLocation) || e.Button != MouseButtons.Left ||
                sender.Viewport.Zoom < 0.5)
                return base.RespondToMouseDown(sender, e);
            _mouseClickInside = true;
            sender.Invalidate();
            return GH_ObjectResponse.Capture;
        }

        /// <summary>
        ///     This method subscribes to mouse up event in the document.
        ///     If a mouse down event inside the button area happened before, this will invoke the button clicked event.
        ///     The background color of the button will be set to normal again inside the render function.
        /// </summary>
        public override GH_ObjectResponse RespondToMouseUp(
            GH_Canvas sender,
            GH_CanvasMouseEvent e)
        {
            if (_mouseClickInside)
            {
                Owner.RecordUndoEvent("Update Selection");
                MouseLeftButtonReleasedInsideRectangleEvent.Invoke(this, new EventArgs());
                _mouseClickInside = false;
                _mouseHoverInside = _buttonRectangleF.Contains(e.CanvasLocation);
                sender.Invalidate();
                return GH_ObjectResponse.Release;
            }

            return GH_ObjectResponse.Ignore;
        }

        /// <summary>
        ///     This method subscribes to mouse moves inside the general boundary of the component area.
        ///     This is used in the render function to determinate if the button should be highlighted.
        /// </summary>
        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button != MouseButtons.None)
                return GH_ObjectResponse.Ignore;
            var isInside = _buttonRectangleF.Contains(GH_Convert.ToPoint(e.CanvasLocation));
            if (_mouseHoverInside != isInside)
                sender.Invalidate();
            _mouseHoverInside = isInside;

            if (_mouseHoverInside)
                return GH_ObjectResponse.Capture;

            return GH_ObjectResponse.Release;
        }

        public event EventHandler MouseLeftButtonReleasedInsideRectangleEvent;
    }
}