/*
 * Copyright (C) 2009 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using System;
using System.Collections.Generic;

namespace CropImage
{
    public class CropImageView : ImageViewTouchBase
    {
        #region Private members

        private List<HighlightView> hightlightViews = new List<HighlightView>();
        private HighlightView mMotionHighlightView = null;
        private float mLastX;
        private float mLastY;
        private global::CropImage.HighlightView.HitPosition mMotionEdge;
        private Context mContext;

        #endregion

        #region Constructor

        public CropImageView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            this.mContext = context;
        }

        #endregion

        #region Public methods

        public void ClearHighlightViews()
        {
            this.hightlightViews.Clear();
        }

        public void AddHighlightView(HighlightView hv)
        {
            hightlightViews.Add(hv);
            Invalidate();
        }

        #endregion

        #region Overrides

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            for (int i = 0; i < hightlightViews.Count; i++)
            {
                hightlightViews[i].Draw(canvas);
            }
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);

            if (bitmapDisplayed.Bitmap != null)
            {
                foreach (var hv in hightlightViews)
                {
                    hv.matrix.Set(ImageMatrix);
                    hv.Invalidate();

                    if (hv.Focused)
                    {
                        centerBasedOnHighlightView(hv);
                    }
                }
            }
        }

        protected override void ZoomTo(float scale, float centerX, float centerY)
        {
            base.ZoomTo(scale, centerX, centerY);
            foreach (var hv in hightlightViews)
            {
                hv.matrix.Set(ImageMatrix);
                hv.Invalidate();
            }
        }

        protected override void ZoomIn()
        {
            base.ZoomIn();
            foreach (var hv in hightlightViews)
            {
                hv.matrix.Set(ImageMatrix);
                hv.Invalidate();
            }
        }

        protected override void ZoomOut()
        {
            base.ZoomOut();
            foreach (var hv in hightlightViews)
            {
                hv.matrix.Set(ImageMatrix);
                hv.Invalidate();
            }
        }

        protected override void PostTranslate(float deltaX, float deltaY)
        {
            base.PostTranslate(deltaX, deltaY);
            for (int i = 0; i < hightlightViews.Count; i++)
            {
                HighlightView hv = hightlightViews[i];
                hv.matrix.PostTranslate(deltaX, deltaY);
                hv.Invalidate();
            }
        }

        // According to the event's position, change the focus to the first
        // hitting cropping rectangle.
        private void recomputeFocus(MotionEvent ev)
        {
            for (int i = 0; i < hightlightViews.Count; i++)
            {
                HighlightView hv = hightlightViews[i];
                hv.Focused = false;
                hv.Invalidate();
            }

            for (int i = 0; i < hightlightViews.Count; i++)
            {
                HighlightView hv = hightlightViews[i];

                var edge = hv.GetHit(ev.GetX(), ev.GetY());
                if (edge != global::CropImage.HighlightView.HitPosition.None)
                {
                    if (!hv.Focused)
                    {
                        hv.Focused = true;
                        hv.Invalidate();
                    }
                    break;
                }
            }

            Invalidate();
        }

        public override bool OnTouchEvent(MotionEvent ev)
        {
            CropImage cropImage = (CropImage)mContext;
            if (cropImage.Saving)
            {
                return false;
            }

            switch (ev.Action)
            {
                case MotionEventActions.Down:
                    if (cropImage.WaitingToPick)
                    {
                        recomputeFocus(ev);
                    }
                    else
                    {
                        for (int i = 0; i < hightlightViews.Count; i++)
                        {
                            HighlightView hv = hightlightViews[i];
                            var edge = hv.GetHit(ev.GetX(), ev.GetY());
                            if (edge != global::CropImage.HighlightView.HitPosition.None)
                            {
                                mMotionEdge = edge;
                                mMotionHighlightView = hv;
                                mLastX = ev.GetX();
                                mLastY = ev.GetY();
                                mMotionHighlightView.Mode = 
                                    (edge == global::CropImage.HighlightView.HitPosition.Move)
                                    ? HighlightView.ModifyMode.Move
                                    : HighlightView.ModifyMode.Grow;
                                break;
                            }
                        }
                    }
                    break;

                case MotionEventActions.Up:
                    if (cropImage.WaitingToPick)
                    {
                        for (int i = 0; i < hightlightViews.Count; i++)
                        {
                            HighlightView hv = hightlightViews[i];

                            if (hv.Focused)
                            {
                                cropImage.Crop = hv;
                                for (int j = 0; j < hightlightViews.Count; j++)
                                {
                                    if (j == i)
                                    {
                                        continue;
                                    }
                                    hightlightViews[j].Hidden = true;
                                }

                                centerBasedOnHighlightView(hv);
                                ((CropImage)mContext).WaitingToPick = false;
                                return true;
                            }
                        }
                    }
                    else if (mMotionHighlightView != null)
                    {
                        centerBasedOnHighlightView(mMotionHighlightView);
                        mMotionHighlightView.Mode = HighlightView.ModifyMode.None;
                    }

                    mMotionHighlightView = null;
                    break;

                case MotionEventActions.Move:
                    if (cropImage.WaitingToPick)
                    {
                        recomputeFocus(ev);
                    }
                    else if (mMotionHighlightView != null)
                    {
                        mMotionHighlightView.HandleMotion(mMotionEdge,
                                                          ev.GetX() - mLastX,
                                                          ev.GetY() - mLastY);
                        mLastX = ev.GetX();
                        mLastY = ev.GetY();

                        if (true)
                        {
                            // This section of code is optional. It has some user
                            // benefit in that moving the crop rectangle against
                            // the edge of the screen causes scrolling but it means
                            // that the crop rectangle is no longer fixed under
                            // the user's finger.
                            ensureVisible(mMotionHighlightView);
                        }
                    }
                    break;
            }

            switch (ev.Action)
            {
                case MotionEventActions.Up:
                    Center(true, true);
                    break;
                case MotionEventActions.Move:
                    // if we're not zoomed then there's no point in even allowing
                    // the user to move the image around.  This call to center puts
                    // it back to the normalized location (with false meaning don't
                    // animate).
                    if (GetScale() == 1F)
                    {
                        Center(true, true);
                    }
                    break;
            }

            return true;
        }

        #endregion

        #region Private helpers

        // Pan the displayed image to make sure the cropping rectangle is visible.
        private void ensureVisible(HighlightView hv)
        {
            Rect r = hv.DrawRect;

            int panDeltaX1 = Math.Max(0, IvLeft - r.Left);
            int panDeltaX2 = Math.Min(0, IvRight - r.Right);

            int panDeltaY1 = Math.Max(0, IvTop - r.Top);
            int panDeltaY2 = Math.Min(0, IvBottom - r.Bottom);

            int panDeltaX = panDeltaX1 != 0 ? panDeltaX1 : panDeltaX2;
            int panDeltaY = panDeltaY1 != 0 ? panDeltaY1 : panDeltaY2;

            if (panDeltaX != 0 || panDeltaY != 0)
            {
                PanBy(panDeltaX, panDeltaY);
            }
        }

        // If the cropping rectangle's size changed significantly, change the
        // view's center and scale according to the cropping rectangle.
        private void centerBasedOnHighlightView(HighlightView hv)
        {
            Rect drawRect = hv.DrawRect;

            float width = drawRect.Width();
            float height = drawRect.Height();

            float thisWidth = Width;
            float thisHeight = Height;

            float z1 = thisWidth / width * .6F;
            float z2 = thisHeight / height * .6F;

            float zoom = Math.Min(z1, z2);
            zoom = zoom * this.GetScale();
            zoom = Math.Max(1F, zoom);
            if ((Math.Abs(zoom - GetScale()) / zoom) > .1)
            {
                float[] coordinates = new float[]
                {
                    hv.CropRect.CenterX(),
					hv.CropRect.CenterY()
				};

                ImageMatrix.MapPoints(coordinates);
                ZoomTo(zoom, coordinates[0], coordinates[1], 300F);
            }

            ensureVisible(hv);
        }

        #endregion
    }
}
