﻿using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
using Esri.ArcGISRuntime.Security;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using UIKit;

namespace Esri.ArcGISRuntime.Toolkit.SampleApp
{
    public partial class ViewController : UIViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        private RuntimeImage _infoIcon = null;
        private RuntimeImage InfoIcon => _infoIcon;

        public override async void ViewDidLoad()
        {
            base.ViewDidLoad();


            // Used in Callout to see feature details in PopupViewer
            _infoIcon = await UIImage.FromBundle("info.png")?.ToRuntimeImageAsync();

            // Used to demonstrate display of EditSummary in PopupViewer
            // Provides credentials to token-secured layer that has editor-tracking enabled
            AuthenticationManager.Current.ChallengeHandler = new ChallengeHandler(async (info) =>
            {
                return await AuthenticationManager.Current.GenerateCredentialAsync(info.ServiceUri, "user1", "user1");
            });

            mapView.GeoViewTapped += mapView_GeoViewTapped;

            // Webmap configured with Popup
            mapView.Map = new Map(new Uri("https://www.arcgis.com/home/item.html?id=d4fe39d300c24672b1821fa8450b6ae2"));

            scaleLine.MapView = mapView;
            compass.GeoView = mapView;
            compass.AutoHide = false;
            popupViewer.BackgroundColor = UIColor.White;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            // Attach the scaleline to always sit right on top of the attribution text
            scaleLine.BottomAnchor.ConstraintEqualTo(mapView.AttributionTopAnchor, -10).Active = true;
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }
        private async void mapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            Exception error = null;
            try
            {
                var result = await mapView.IdentifyLayersAsync(e.Position, 3, false);
                // Retrieves or builds Popup from IdentifyLayerResult
                var popup = GetPopup(result);

                // Displays callout and updates visibility of PopupViewer
                if (popup != null)
                {
                    var callout = new CalloutDefinition(popup.GeoElement);
                    callout.Tag = popup;
                    callout.ButtonImage = InfoIcon;
                    callout.OnButtonClick = new Action<object>((s) =>
                    {
                        popupViewer.Hidden = false;
                        popupViewer.PopupManager = new PopupManager(popup);
                    });
                    mapView.ShowCalloutForGeoElement(popup.GeoElement, e.Position, callout);
                }
                else
                {
                    popupViewer.PopupManager = null;
                    popupViewer.Hidden = true;
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            if (error != null)
            {
                var alert = UIAlertController.Create(error.GetType().Name, error.Message, UIAlertControllerStyle.Alert);
                alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
                this.PresentViewController(alert, true, null);
            }
        }

        private Popup GetPopup(IdentifyLayerResult result)
        {
            if (result == null)
            {
                return null;
            }

            var popup = result.Popups.FirstOrDefault();
            if (popup != null)
            {
                return popup;
            }

            var geoElement = result.GeoElements.FirstOrDefault();
            if (geoElement != null)
            {
                if (result.LayerContent is IPopupSource)
                {
                    var popupDefinition = ((IPopupSource)result.LayerContent).PopupDefinition;
                    if (popupDefinition != null)
                    {
                        return new Popup(geoElement, popupDefinition);
                    }
                }

                return Popup.FromGeoElement(geoElement);
            }

            return null;
        }

        private Popup GetPopup(IEnumerable<IdentifyLayerResult> results)
        {
            if (results == null)
            {
                return null;
            }
            foreach (var result in results)
            {
                var popup = GetPopup(result);
                if (popup != null)
                {
                    return popup;
                }

                foreach (var subResult in result.SublayerResults)
                {
                    popup = GetPopup(subResult);
                    if (popup != null)
                    {
                        return popup;
                    }
                }
            }

            return null;
        }
    }
}