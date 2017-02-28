using System;
using System.Collections.Generic;
using System.Linq;

using Intel.CFC.Plugin;

using Q42.HueApi.Interfaces;
using Q42.HueApi;
using Q42.HueApi.NET;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.HSB;

namespace UniteHuePlugin
{
    public class HueBridge
    {
        private string bridgeId; //comes from M-SEARCH response
        private string name;
        private string ip;

        public HueBridge(string bridgeId, string name, string ip)
        {
            this.bridgeId = bridgeId;
            this.name = name;
            this.ip = ip;
        }

        public string getBridgeId()
        {
            return bridgeId;
        }

        public string getName()
        {
            return name;
        }

        public string getIp()
        {
            return ip;
        }
    }

    public class HueLight
    {
        private string lightId;
        private string name;

        public HueLight(string id, string name)
        {
            this.lightId = id;
            this.name = name;
        }

        public string getLightId()
        {
            return lightId;
        }

        public string getName()
        {
            return name;
        }
    }

    public enum ActionEnum
    {
        SEARCH_BRIDGES = 1,
        CONNECT_BRIDGE,
        SELECT_LIGHT,
        LIGHT_ON,
        LIGHT_OFF,
        LIGHT_INCREASE,
        LIGHT_DECREASE,
        LIGHT_CHANGE_COLOR
    }

    public struct ButtonPressAction
    {
        public ActionEnum action;
        public string value;

        public ButtonPressAction (ActionEnum act, string val)
        {
            action = act;
            value = val;
        }
    }


    public class UniteHuePlugin : CFCPlugin
    {
        PluginUI UI = null;
        PluginInfo pluginDetails = new PluginInfo();

        PluginUIElementGroup elementsGroup = new PluginUIElementGroup();

        byte[] hueLogoImage = new byte[0];
        string HubText = "";

        //this dictionary keeps the actions that should happen when a button/image is clicked in the UI
        static Dictionary<Guid, ButtonPressAction> ButtonPressActionDictionary = new Dictionary<Guid, ButtonPressAction>();

        static List<HueBridge> foundHueBridges = new List<HueBridge>();
        static List<HueLight> foundHueLights = new List<HueLight>();

        static HueBridge connectedBridge;
        static HueLight selectedLight;

        static string appKey;
        static string appName = "UniteHuePlugin";
        static string deviceName = "UniteBridgeDevice";
        static ILocalHueClient client;


        public void SimpleTestPlugin()
        {
            //TODO: load previous session data from file

            //Define plugin details
            pluginDetails.Name = "Unite Hue Plugin";
            pluginDetails.Id = new Guid("12345678-1234-1234-1234-123456781234");
            pluginDetails.Description = "Intel Unite Plugin For Controlling Philips Hue";
            pluginDetails.Copyright = "Copyright (C) 2017 Intel Corporation - All Rights Reserved";
            pluginDetails.Company = "Intel Corporation";

            //the image show with ShowHubToast() 
            hueLogoImage = ResourceToBytes(new Uri("/UniteHuePlugin;component/images/huelogo.png", System.UriKind.Relative));

            //UI elements
            UI = new PluginUI();
            UI.pluginInfo = pluginDetails;
            UI.Groups = new List<PluginUIElementGroup>();
            UpdateUIGroups();

        }
        public override void Load()
        {
            //Do nothing
            LogMessage("Plugin Loaded", null);
            SimpleTestPlugin();
        }
        public override void UnLoad()
        {
            //Do nothing
            LogMessage("Plugin Unloaded", null);
        }
        public override void UserConnected(UserEventArgs e)
        {
            //Do nothing
            ShowHubToast(e.TargetUser.Name + " has joined!", new byte[0], 3);
            LogMessage("Plugin User Connect", null);
        }
        public override void UserDisconnected(UserEventArgs e)
        {
            //Do nothing
            ShowHubToast(e.TargetUser.Name + " has disconnected!", new byte[0], 3);
            LogMessage("Plugin User Disconnect Loaded", null);
        }

        public override void UserPresentationStart(UserEventArgs e)
        {
            //Do nothing
            LogMessage("Plugin Presentation Started", null);
        }
        public override void UserPresentationEnd(UserEventArgs e)
        {
            //Do nothing
            LogMessage("Plugin Presentation End", null);
        }
        public override void UIElementEvent(UIEventArgs e)
        {
            LogMessage("Plugin Received UI Event: " + e.ElementId.ToString(), null);
            Guid receivedGuid = e.ElementId;

            ButtonPressAction buttonPressAction = ButtonPressActionDictionary[receivedGuid];
            if (buttonPressAction.action == ActionEnum.SEARCH_BRIDGES)
            {
                HubText = "Searching Bridges...";
                ShowHubToast(HubText, hueLogoImage, 2);
                LogMessage(HubText, null);
                
                connectedBridge = null;
                selectedLight = null;

                SearchBridges();
            }
            else if (buttonPressAction.action == ActionEnum.CONNECT_BRIDGE)
            {
                string bridgeId = buttonPressAction.value;

                HubText = "Connecting to Bridge: " + bridgeId;
                ShowHubToast(HubText, hueLogoImage, 2);
                LogMessage(HubText, null);

                HueBridge bridge = foundHueBridges.First(item => item.getBridgeId() == bridgeId);

                ConnectToBridge(bridge);
            }
            else if (buttonPressAction.action == ActionEnum.SELECT_LIGHT)
            {
                string lightId = buttonPressAction.value;

                HubText = "Selected Light: " + lightId;
                ShowHubToast(HubText, hueLogoImage, 2);
                LogMessage(HubText, null);

                selectedLight = foundHueLights.First(item => item.getLightId() == lightId);
            }
            else if (buttonPressAction.action == ActionEnum.LIGHT_ON)
            {
                HubText = "Turning on Light: " + selectedLight.getLightId();
                ShowHubToast(HubText, hueLogoImage, 2);
                LogMessage(HubText, null);

                TurnOnLight(selectedLight);
            }
            else if (buttonPressAction.action == ActionEnum.LIGHT_OFF)
            {
                HubText = "Turning off Light: " + selectedLight.getLightId();
                ShowHubToast(HubText, hueLogoImage, 2);
                LogMessage(HubText, null);

                TurnOffLight(selectedLight);
            }
            else if (buttonPressAction.action == ActionEnum.LIGHT_INCREASE)
            {
                HubText = "Increasing Brightness of Light: " + selectedLight.getLightId();
                ShowHubToast(HubText, hueLogoImage, 2);
                LogMessage(HubText, null);

                IncreaseBrightness(selectedLight);
            }
            else if (buttonPressAction.action == ActionEnum.LIGHT_DECREASE)
            {
                HubText = "Decreasing Brightness of Light: : " + selectedLight.getLightId();
                ShowHubToast(HubText, hueLogoImage, 2);
                LogMessage(HubText, null);

                DecreaseBrightness(selectedLight);
            }
            else if (buttonPressAction.action == ActionEnum.LIGHT_CHANGE_COLOR)
            {
                HubText = "Changing Color of Light " + selectedLight.getLightId() + " to " + buttonPressAction.value;
                ShowHubToast(HubText, hueLogoImage, 2);
                LogMessage(HubText, null);

                string color = buttonPressAction.value;

                ChangeLightColor(selectedLight, color);
            }
            else
            {
                HubText = "Invalid button press action";
                ShowHubToast(HubText, hueLogoImage, 2);
                LogMessage("Invalid button press action requested: " + buttonPressAction.action, null);
            }

            UpdateUIGroups();

            FireHubTextUpdated();
            FireUIUpdated();
        }
        public override PluginUI GetUI(UserEventArgs e)
        {
            return UI;
        }
        public override PluginInfo GetPluginInfo()
        {
            return pluginDetails;
        }
        public override string GetHubText()
        {
            return HubText;
        }

        async void SearchBridges()
        {
            //search bridges at https://www.meethue.com/api/nupnp
            //IBridgeLocator locator = new HttpBridgeLocator();
            //scans network using multicast SSDP packets
            IBridgeLocator locator = new SSDPBridgeLocator();

            IEnumerable<Q42.HueApi.Models.Bridge.LocatedBridge> locatedBridges = await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5));
            
            foundHueBridges.Clear();
            foundHueLights.Clear();
            foreach (var locatedBridge in locatedBridges)
            {
                foundHueBridges.Add(new HueBridge(locatedBridge.BridgeId, locatedBridge.BridgeId, locatedBridge.IpAddress));
            }

            string message = "Found Hue bridges count: " + locatedBridges.Count();
            LogMessage(message, null);
            ShowHubToast(message, hueLogoImage, 2);

            UpdateUIGroups();
            FireUIUpdated();
        }
        
        Guid createButtonPressAction(ActionEnum action, string value ="")
        {
            Guid g = Guid.NewGuid();

            ButtonPressActionDictionary.Add(g, new ButtonPressAction(action, value));

            return g;
        }

        void UpdateUIGroups()
        {
            ButtonPressActionDictionary.Clear(); //Previous GUIDs and actions are no longer valid. It will be filled from scratch.
            UI.Groups.Clear();

            elementsGroup.GroupName = "Philips Hue";
            elementsGroup.ImageBytes = ResourceToBytes(new Uri("/UniteHuePlugin;component/images/hueplugin.png", UriKind.Relative));
            elementsGroup.UIElements = new List<PluginUIElement>();

            elementsGroup.UIElements.Add(new PluginUIElement(createButtonPressAction(ActionEnum.SEARCH_BRIDGES), UIElementType.Button, "Search Bridges", "", ResourceToBytes(new Uri("/UniteHuePlugin;component/images/search.png", System.UriKind.Relative))));

            LogMessage("foundHueBridges.Count: " + foundHueBridges.Count(), null);
            if (foundHueBridges.Count > 0)
            {
                foreach (HueBridge hueBridge in foundHueBridges)
                {
                    LogMessage("hueBridge: " + hueBridge.getBridgeId(), null);
                    if (connectedBridge != null && hueBridge.getBridgeId() == connectedBridge.getBridgeId())
                    {
                        elementsGroup.UIElements.Add(new PluginUIElement(createButtonPressAction(ActionEnum.CONNECT_BRIDGE, hueBridge.getBridgeId()), UIElementType.Button, hueBridge.getName(), "", ResourceToBytes(new Uri("/UniteHuePlugin;component/images/bridge-connected.png", System.UriKind.Relative))));
                    }
                    else
                    {
                        elementsGroup.UIElements.Add(new PluginUIElement(createButtonPressAction(ActionEnum.CONNECT_BRIDGE, hueBridge.getBridgeId()), UIElementType.Button, hueBridge.getName(), "", ResourceToBytes(new Uri("/UniteHuePlugin;component/images/bridge.png", System.UriKind.Relative))));
                    }
                }
                
                if (foundHueLights.Count > 0)
                {
                    foreach (HueLight hueLight in foundHueLights)
                    {
                        if (selectedLight != null && hueLight.getLightId() == selectedLight.getLightId())
                        {
                            elementsGroup.UIElements.Add(new PluginUIElement(createButtonPressAction(ActionEnum.SELECT_LIGHT, hueLight.getLightId()), UIElementType.Button, hueLight.getName(), "", ResourceToBytes(new Uri("/UniteHuePlugin;component/images/light-selected.png", System.UriKind.Relative))));
                        }
                        else
                        {
                            elementsGroup.UIElements.Add(new PluginUIElement(createButtonPressAction(ActionEnum.SELECT_LIGHT, hueLight.getLightId()), UIElementType.Button, hueLight.getName(), "", ResourceToBytes(new Uri("/UniteHuePlugin;component/images/light.png", System.UriKind.Relative))));
                        }
                    }
                }

                if (selectedLight != null)
                {
                    elementsGroup.UIElements.Add(new PluginUIElement(createButtonPressAction(ActionEnum.LIGHT_ON), UIElementType.Button, "Turn ON", "", ResourceToBytes(new Uri("/UniteHuePlugin;component/images/turnon.png", System.UriKind.Relative))));
                    elementsGroup.UIElements.Add(new PluginUIElement(createButtonPressAction(ActionEnum.LIGHT_OFF), UIElementType.Button, "Turn OFF", "", ResourceToBytes(new Uri("/UniteHuePlugin;component/images/turnoff.png", System.UriKind.Relative))));

                    elementsGroup.UIElements.Add(new PluginUIElement(createButtonPressAction(ActionEnum.LIGHT_INCREASE), UIElementType.Button, "Increase Brightness", "", ResourceToBytes(new Uri("/UniteHuePlugin;component/images/increase.png", System.UriKind.Relative))));
                    elementsGroup.UIElements.Add(new PluginUIElement(createButtonPressAction(ActionEnum.LIGHT_DECREASE), UIElementType.Button, "Decrease Brightness", "", ResourceToBytes(new Uri("/UniteHuePlugin;component/images/decrease.png", System.UriKind.Relative))));

                    elementsGroup.UIElements.Add(new PluginUIElement(createButtonPressAction(ActionEnum.LIGHT_CHANGE_COLOR, "FF0000"), UIElementType.Button, "Red", "", ResourceToBytes(new Uri("/UniteHuePlugin;component/images/light-red.png", System.UriKind.Relative))));
                    elementsGroup.UIElements.Add(new PluginUIElement(createButtonPressAction(ActionEnum.LIGHT_CHANGE_COLOR, "0000FF"), UIElementType.Button, "Blue", "", ResourceToBytes(new Uri("/UniteHuePlugin;component/images/light-blue.png", System.UriKind.Relative))));
                    elementsGroup.UIElements.Add(new PluginUIElement(createButtonPressAction(ActionEnum.LIGHT_CHANGE_COLOR, "FFFFFF"), UIElementType.Button, "White", "", ResourceToBytes(new Uri("/UniteHuePlugin;component/images/light-white.png", System.UriKind.Relative))));
                }
            }
            UI.Groups.Add(elementsGroup);
        }



        async void ConnectToBridge(HueBridge bridge)
        {
            client = new LocalHueClient(bridge.getIp());

            try
            {
                appKey = await client.RegisterAsync(appName, deviceName); //Save the app key for later use

                client.Initialize(appKey);

            } catch (System.Exception exp)
            {
                HubText = "ERROR: " + exp.Message;
                ShowHubToast(HubText, hueLogoImage, 2);
                LogMessage(HubText, null);

                client = null;
            }
            
            //client = await RegisterApplication(bridge.getIp());

            if (client != null)
            {
                connectedBridge = bridge;

                GetLights();
            }
        }

        async void GetLights()
        {
            HubText = "Getting Hue Lights...";
            ShowHubToast(HubText, hueLogoImage, 2);
            LogMessage(HubText, null);

            var lights = await client.GetLightsAsync();
            
            HubText = "Found Lights: " + lights.Count();
            ShowHubToast(HubText, hueLogoImage, 2);
            LogMessage(HubText, null);

            foundHueLights.Clear();
            foreach (var light in lights)
            {
                foundHueLights.Add(new HueLight(light.Id, light.Name));
            }

            UpdateUIGroups();
            FireUIUpdated();
        }
        

        void StartLightEffect()
        {
        }

        void SendCommandToClient(LightCommand command, string lightId)
        {
            if (client != null)
            {
                client.SendCommandAsync(command, new List<string> { lightId });
            } else
            {
                HubText = "Client not set";
                ShowHubToast(HubText, hueLogoImage, 2);
                LogMessage(HubText, null);
            }
        }

        async void TurnOnLight(HueLight light)
        {
            var command = new LightCommand();
            command.On = true;

            SendCommandToClient(command, light.getLightId());
        }

        async void TurnOffLight(HueLight light)
        {
            var command = new LightCommand();
            command.On = false;

            SendCommandToClient(command, light.getLightId());
        }

        async void IncreaseBrightness(HueLight light)
        {
            var command = new LightCommand();
            command.BrightnessIncrement = 40;

            SendCommandToClient(command, light.getLightId());
        }

        async void DecreaseBrightness(HueLight light)
        {
            var command = new LightCommand();
            command.BrightnessIncrement = -40;

            SendCommandToClient(command, light.getLightId());
        }

        async void ChangeLightColor(HueLight light, string color)
        {
            var command = new LightCommand();
            command.TurnOn().SetColor(new RGBColor(color));

            SendCommandToClient(command, light.getLightId());
        }

    }

}