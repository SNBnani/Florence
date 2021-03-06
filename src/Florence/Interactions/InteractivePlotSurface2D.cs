﻿/*
 * Florence - A charting library for .NET
 * 
 * InteractivePlotSurface2D.cs
 * Copyright (C) 2003-2006 Matt Howlett and others.
 * Copyright (C) 2003-2013 Hywel Thomas
 * Copyright (C) 2013 Scott Stephens
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 * 
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of Florence nor the names of its contributors may
 *    be used to endorse or promote products derived from this software without
 *    specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
 * INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE
 * OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
 * OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;

namespace Florence
{

    /// <summary>
    /// Interactive plot surface2 d.
    /// </summary> <summary>
    /// Extends PlotSurface2D with Interactions which allow the user to change
    /// the plot using mouse and keyboard inputs.  A common mechanism for mouse
    /// and keyboard inputs is used, so that the platform-specific input handlers
    /// convert to this common format and then call the interaction code here.
    /// This maximises the amount of common code that can be used.
    /// </summary>
    public class InteractivePlotSurface2D : PlotSurface2D
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public InteractivePlotSurface2D()
            : base()
        {
            // Create empty InteractionOccurred and PreRefresh Event handlers
            this.InteractionOccurred += new InteractionHandler(OnInteractionOccurred);
            this.PreRefresh += new PreRefreshHandler(OnPreRefresh);
        }

        public event Action<Rectangle> DrawQueued;
        public event Action RefreshRequested;

        /// <summary>
        /// Clear the plot and reset to default values.
        /// </summary>
        public new void Clear()
        {
            ClearAxisCache();
            interactions.Clear();
            base.Clear();
        }

        private CursorType plotcursor;

        public CursorType plotCursor
        {
            get { return plotcursor; }
            set { plotcursor = value; }
        }

        #region Axis Cache and Range utilities

        private Axis xAxis1ZoomCache_;		// copies of current axes,
        private Axis yAxis1ZoomCache_;		// saved for restoring the
        private Axis xAxis2ZoomCache_;		// original dimensions after
        private Axis yAxis2ZoomCache_;		// zooming, etc

        /// <summary>
        /// Caches the current axes
        /// </summary>
        public void CacheAxes()
        {
            if (xAxis1ZoomCache_ == null && xAxis2ZoomCache_ == null &&
                yAxis1ZoomCache_ == null && yAxis2ZoomCache_ == null)
            {
                if (this.XAxis1 != null)
                {
                    xAxis1ZoomCache_ = (Axis)this.XAxis1.Clone();
                }
                if (this.XAxis2 != null)
                {
                    xAxis2ZoomCache_ = (Axis)this.XAxis2.Clone();
                }
                if (this.YAxis1 != null)
                {
                    yAxis1ZoomCache_ = (Axis)this.YAxis1.Clone();
                }
                if (this.YAxis2 != null)
                {
                    yAxis2ZoomCache_ = (Axis)this.YAxis2.Clone();
                }
            }
        }

        /// <summary>
        /// Sets axes to be those saved in the cache.
        /// </summary>
        public void SetOriginalDimensions()
        {
            if (xAxis1ZoomCache_ != null)
            {
                this.XAxis1 = xAxis1ZoomCache_;
                this.XAxis2 = xAxis2ZoomCache_;
                this.YAxis1 = yAxis1ZoomCache_;
                this.YAxis2 = yAxis2ZoomCache_;

                xAxis1ZoomCache_ = null;
                xAxis2ZoomCache_ = null;
                yAxis1ZoomCache_ = null;
                yAxis2ZoomCache_ = null;
            }
        }

        protected void ClearAxisCache()
        {
            xAxis1ZoomCache_ = null;
            yAxis1ZoomCache_ = null;
            xAxis2ZoomCache_ = null;
            yAxis2ZoomCache_ = null;
        }

        /// <summary>
        /// Translate all PlotSurface X-Axes by shiftProportion
        /// </summary>
        public void TranslateXAxes(double shiftProportion)
        {
            if (XAxis1 != null)
            {
                XAxis1.TranslateRange(shiftProportion);
            }
            if (XAxis2 != null)
            {
                XAxis2.TranslateRange(shiftProportion);
            }
        }

        /// <summary>
        /// Translate all PlotSurface Y-Axes by shiftProportion
        /// </summary>
        public void TranslateYAxes(double shiftProportion)
        {
            if (YAxis1 != null)
            {
                YAxis1.TranslateRange(shiftProportion);
            }
            if (YAxis2 != null)
            {
                YAxis2.TranslateRange(shiftProportion);
            }
        }

        /// <summary>
        /// Zoom all PlotSurface X-Axes about focusPoint by zoomProportion 
        /// </summary>
        public void ZoomXAxes(double zoomProportion, double focusRatio)
        {
            if (XAxis1 != null)
            {
                XAxis1.IncreaseRange(zoomProportion, focusRatio);
            }
            if (XAxis2 != null)
            {
                XAxis2.IncreaseRange(zoomProportion, focusRatio);
            }
        }

        /// <summary>
        /// Zoom all PlotSurface Y-Axes about focusPoint by zoomProportion 
        /// </summary>
        public void ZoomYAxes(double zoomProportion, double focusRatio)
        {
            if (YAxis1 != null)
            {
                YAxis1.IncreaseRange(zoomProportion, focusRatio);
            }
            if (YAxis2 != null)
            {
                YAxis2.IncreaseRange(zoomProportion, focusRatio);
            }
        }

        /// <summary>
        /// Define all PlotSurface X-Axes to minProportion, maxProportion
        /// </summary>
        public void DefineXAxes(double minProportion, double maxProportion)
        {
            if (XAxis1 != null)
            {
                XAxis1.DefineRange(minProportion, maxProportion, true);
            }
            if (XAxis2 != null)
            {
                XAxis2.DefineRange(minProportion, maxProportion, true);
            }
        }

        /// <summary>
        /// Define all PlotSurface Y-Axes to minProportion, maxProportion
        /// </summary>
        public void DefineYAxes(double minProportion, double maxProportion)
        {
            if (YAxis1 != null)
            {
                YAxis1.DefineRange(minProportion, maxProportion, true);
            }
            if (YAxis2 != null)
            {
                YAxis2.DefineRange(minProportion, maxProportion, true);
            }
        }
        #endregion

        #region Add/Remove Interaction

        private ArrayList interactions = new ArrayList();

        /// <summary>
        /// Adds a specific interaction to the PlotSurface2D
        /// </summary>
        /// <param name="i">the interaction to add.</param>
        public void AddInteraction(Interaction i)
        {
            interactions.Add(i);
        }

        /// <summary>
        /// Remove a previously added interaction
        /// </summary>
        /// <param name="i">interaction to remove</param>
        public void RemoveInteraction(Interaction i)
        {
            interactions.Remove(i);
        }

        #endregion Add/Remove Interaction

        #region InteractivePlotSurface Events

        /// An Event is raised to notify clients that an Interaction has modified
        /// the PlotSurface, and a separate Event is also raised prior to a call
        /// to refresh the PlotSurface.	 Currently, the conditions for raising
        /// both Events are the same (ie the PlotSurface has been modified)

        /// <summary>
        /// InteractionOccurred event signature
        /// TODO: expand this to include information about the event. 
        /// </summary>
        /// <param name="sender"></param>
        public delegate void InteractionHandler(object sender);


        /// <summary>
        /// Event raised when an interaction modifies the PlotSurface
        /// </summary>
        public event InteractionHandler InteractionOccurred;


        /// <summary>
        /// Default handler called when Interaction modifies PlotSurface
        /// Override this, or add handler to InteractionOccurred event.
        /// </summary>
        /// <param name="sender"></param>
        protected void OnInteractionOccurred(object sender)
        {
        }


        /// <summary>
        /// PreRefresh event handler signature
        /// </summary>
        /// <param name="sender"></param>
        public delegate void PreRefreshHandler(object sender);


        /// <summary>
        /// Event raised prior to Refresh call
        /// </summary>
        public event PreRefreshHandler PreRefresh;


        /// <summary>
        /// Default handler for PreRefresh
        /// Override this, or add handler to PreRefresh event.
        /// </summary>
        /// <param name="sender"></param>
        protected void OnPreRefresh(object sender)
        {
        }

        #endregion

        #region PlotSurface Interaction handlers

        // The methods which are called by the platform-specific event handlers and which in turn
        // call the individual Interaction handlers for those events. Note that a reference to the
        // PlotSurface is passed as well as the event details, so that Interactions can call the
        // PlotSurface public methods if required (eg to redraw an area of the plotSurface)


        /// <summary>
        /// Handle Draw event for all interactions. Called by platform-specific OnDraw/Paint
        /// </summary>
        public void DoDraw(Graphics g, Rectangle clip)
        {
            base.Draw(g, clip);
            
            foreach (Interaction i in interactions)
            {
                i.DoDraw(g, clip);
            }
        }

        /// <summary>
        /// Handle MouseEnter for all PlotSurface interactions
        /// </summary>
        /// <returns>true if plot has been modified</returns>
        public bool DoMouseEnter(EventArgs args)
        {
            bool modified = false;
            foreach (Interaction i in interactions)
            {
                modified |= i.DoMouseEnter(this);
            }
            ShowCursor(this.plotCursor);	//set by each Interaction
            if (modified)
            {
                InteractionOccurred(this);
                Refresh();
            }
            return (modified);
        }

        /// <summary>
        /// Handle MouseLeave for all PlotSurface interactions
        /// </summary>
        /// <returns>true if plot has been modified</returns>
        public bool DoMouseLeave(EventArgs args)
        {
            bool modified = false;
            foreach (Interaction i in interactions)
            {
                modified |= i.DoMouseLeave(this);
            }
            ShowCursor(this.plotCursor);
            if (modified)
            {
                InteractionOccurred(this);
                Refresh();
            }
            return (modified);
        }

        /// <summary>
        /// Handle MouseDown for all PlotSurface interactions
        /// </summary>
        /// <param name="X">mouse X position</param>
        /// <param name="Y"> mouse Y position</param>
        /// <param name="keys"> mouse and keyboard modifiers</param>
        /// <returns>true if plot has been modified</returns>
        public bool DoMouseDown(int X, int Y, Modifier keys)
        {
            bool modified = false;
            foreach (Interaction i in interactions)
            {
                modified |= i.DoMouseDown(X, Y, keys, this);
            }
            ShowCursor(this.plotCursor);
            if (modified)
            {
                InteractionOccurred(this);
                Refresh();
            }
            return (modified);
        }

        /// <summary>
        /// // Handle MouseUp for all PlotSurface interactions
        /// </summary>
        /// <param name="X">mouse X position</param>
        /// <param name="Y"> mouse Y position</param>
        /// <param name="keys"> mouse and keyboard modifiers</param>
        /// <returns>true if plot has been modified</returns>
        public bool DoMouseUp(int X, int Y, Modifier keys)
        {
            bool modified = false;
            foreach (Interaction i in interactions)
            {
                modified |= i.DoMouseUp(X, Y, keys, this);
            }
            ShowCursor(this.plotCursor);
            if (modified)
            {
                InteractionOccurred(this);
                Refresh();
            }
            return (modified);
        }

        /// <summary>
        /// Handle MouseMove for all PlotSurface interactions
        /// </summary>
        /// <param name="X">mouse X position</param>
        /// <param name="Y"> mouse Y position</param>
        /// <param name="keys"> mouse and keyboard modifiers</param>
        /// <returns>true if plot has been modified</returns>
        public bool DoMouseMove(int X, int Y, Modifier keys)
        {
            bool modified = false;
            foreach (Interaction i in interactions)
            {
                modified |= i.DoMouseMove(X, Y, keys, this);
            }
            ShowCursor(this.plotCursor);
            if (modified)
            {
                InteractionOccurred(this);
                Refresh();
            }
            return (modified);
        }

        /// <summary>
        /// Handle Mouse Scroll (wheel) for all PlotSurface interactions
        /// </summary>
        /// <param name="X">mouse X position</param>
        /// <param name="Y"> mouse Y position</param>
        /// <param name="direction"> scroll direction</param>
        /// <param name="keys"> mouse and keyboard modifiers</param>
        /// <returns>true if plot has been modified</returns>
        public bool DoMouseScroll(int X, int Y, int direction, Modifier keys)
        {
            bool modified = false;
            foreach (Interaction i in interactions)
            {
                modified |= i.DoMouseScroll(X, Y, direction, keys, this);
            }
            ShowCursor(this.plotCursor);
            if (modified)
            {
                InteractionOccurred(this);
                Refresh();
            }
            return (modified);
        }

        /// <summary>
        /// Handle KeyPressed for all PlotSurface interactions
        /// </summary>
        public bool DoKeyPress(Modifier keys, InteractivePlotSurface2D ps)
        {
            bool modified = false;
            foreach (Interaction i in interactions)
            {
                modified |= i.DoKeyPress(keys, this);
            }
            if (modified)
            {
                InteractionOccurred(this);
                Refresh();
            }
            return (modified);
        }


        /// <summary>
        /// Handle KeyReleased for all PlotSurface interactions
        /// </summary>
        public bool DoKeyRelease(Modifier keys, InteractivePlotSurface2D ps)
        {
            bool modified = false;
            foreach (Interaction i in interactions)
            {
                modified |= i.DoKeyRelease(keys, this);
            }
            if (modified)
            {
                InteractionOccurred(this);
                Refresh();
            }
            return (modified);
        }
        #endregion

        #region PlotSurface virtual methods

        /// <summary>
        /// Displays the current plotCursor, set in each interaction
        /// This must be overridden by each implementation so that
        /// the appropriate platform cursor type can be displayed
        /// </summary>
        public virtual void ShowCursor(CursorType plotCursor)
        {
        }

        /// <summary>
        /// Update the entire plot area on the platform-specific output
        /// Override this method for each implementation (Swf, Gtk)
        /// </summary>
        public virtual void Refresh()
        {
            this.PreRefresh(this);

            if (this.RefreshRequested != null)
                this.RefreshRequested();
        }

        /// <summary>
        /// Invalidate rectangle specified. The Paint/OnDraw handler will then to redraw the area
        /// </summary>
        public virtual void QueueDraw(Rectangle dirtyRect)
        {
            if (this.DrawQueued != null)
                this.DrawQueued(dirtyRect);
        }

        /// <summary>
        /// Process window updates immediately
        /// </summary>
        public virtual void ProcessUpdates(bool updateChildren)
        {
        }

        #endregion

    } // class InteractivePlotSurface2D

} // namespace Florence
