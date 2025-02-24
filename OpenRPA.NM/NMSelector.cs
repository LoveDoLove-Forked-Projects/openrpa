﻿using Newtonsoft.Json.Linq;
using OpenRPA.Interfaces;
using OpenRPA.Interfaces.Selector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.NM
{
    public class NMSelector : Selector
    {
        NMElement element { get; set; }
        public NMSelector(string json) : base(json) { }
        public string browser
        {
            get
            {
                if (this.Count == 0) return null;
                var first = this[0];
                var p = first.Properties.Where(x => x.Name == "browser").FirstOrDefault();
                if (p == null) return null;
                return p.Value;
            }
        }
        public NMSelector(NMElement element, NMSelector anchor, bool doEnum, NMElement anchorelement)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            Log.Selector(string.Format("NMselector::AutomationElement::begin {0:mm\\:ss\\.fff}", sw.Elapsed));
            Log.Selector(string.Format("NMselector::GetControlVNMwWalker::end {0:mm\\:ss\\.fff}", sw.Elapsed));


            if(element.xpath == "true")
            {
                element.Refresh();
            }
            NMSelectorItem item;
            if (anchor == null)
            {
                item = new NMSelectorItem(element, true, false);
                item.Enabled = true;
                item.canDisable = false;
                Items.Add(item);
            }
            else
            {
                var anchorarray = anchorelement.cssselector.Split('>');
                var elementarray = element.cssselector.Split('>');
                elementarray = elementarray.Skip(anchorarray.Length).ToArray();
                if(element.xpath.StartsWith(anchorelement.xpath))
                {
                    element.xpath = "" + element.xpath.Substring(anchorelement.xpath.Length);
                    element.cssselector = "";
                } 
                else
                {
                    element.cssselector = string.Join(">", elementarray);
                }
                
            }
            item = new NMSelectorItem(element, false, (anchor != null));
            item.Enabled = true;
            item.canDisable = false;
            Items.Add(item);

            Log.Selector(string.Format("NMselector::EnumNeededProperties::end {0:mm\\:ss\\.fff}", sw.Elapsed));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
        public override IElement[] GetElements(IElement fromElement = null, int maxresults = 1)
        {
            return NMSelector.GetElementsWithuiSelector(this, fromElement, maxresults);
        }
        public static NMElement[] GetElementsWithuiSelector(NMSelector selector, IElement fromElement = null, int maxresults = 1)
        {
            var results = new List<NMElement>();
            SelectorItem first = null;
            SelectorItem second = null;
            string browser = "";
            SelectorItemProperty p = null;
            if (selector.Count > 1)
            {
                first = selector[0];
                second = selector[1];
                p = first.Properties.Where(x => x.Name == "browser").FirstOrDefault();
                if (p != null) { browser = p.Value; }
            }
            else if (fromElement == null)
            {
                throw new ArgumentException("Invalid select with only 1 child and no anchor");
            }
            else
            {
                second = selector[0];
            }
            p = second.Properties.Where(x => x.Name == "xpath").FirstOrDefault();
            string xpath = "";
            if (p != null) { xpath = p.Value; }
            p = second.Properties.Where(x => x.Name == "cssselector").FirstOrDefault();
            string cssselector = "";
            if (p != null) { cssselector = p.Value; }
            NMElement fromNMElement = fromElement as NMElement;
            string fromcssPath = "";
            string fromxPath = "";
            NativeMessagingMessage subresult = null;

            var getelement = new NativeMessagingMessage("getelements", PluginConfig.debug_console_output, PluginConfig.unique_xpath_ids);

            if (fromElement != null)
            {
                getelement.frameId = fromNMElement.message.frameId;
                getelement.tabid = fromNMElement.message.tabid;
                getelement.windowId = fromNMElement.message.windowId;
                fromcssPath = fromNMElement.cssselector;
                fromxPath = fromNMElement.xpath;

                if (!string.IsNullOrEmpty(selector[0].use_zn) && fromNMElement.zn_id > 0)
                {
                    fromcssPath = "";
                    if (string.IsNullOrEmpty(xpath)) fromcssPath = fromNMElement.cssselector;
                    if (!string.IsNullOrEmpty(fromNMElement.tagname))
                    {
                        // case sensetiv :-(
                        // fromxPath = "//" + fromNMElement.tagname.ToUpperInvariant() + "[@zn_id=\"" + fromNMElement.zn_id + "\"]";
                        fromxPath = "//*[@zn_id=\"" + fromNMElement.zn_id + "\"]";
                    }
                    else
                    {
                        fromxPath = "//*[@zn_id=\"" + fromNMElement.zn_id + "\"]";
                    }
                }
                else if (!string.IsNullOrEmpty(xpath) && !string.IsNullOrEmpty(fromNMElement.xpath))
                {
                    fromcssPath = "";
                    fromxPath = "";
                    xpath = fromNMElement.xpath + xpath;
                }
                else if (!string.IsNullOrEmpty(cssselector) && !string.IsNullOrEmpty(fromNMElement.cssselector))
                {
                    //fromcssPath = "";
                    //fromxPath = "";
                    //cssselector = fromNMElement.cssselector + " > " + cssselector;
                    fromcssPath = fromNMElement.cssselector;
                    fromxPath = fromNMElement.xpath;
                }
            }

            getelement.browser = browser;
            getelement.xPath = xpath;
            getelement.cssPath = cssselector;
            getelement.fromxPath = fromxPath;
            getelement.fromcssPath = fromcssPath;
            if(PluginConfig.wait_for_tab_after_set_value)
            if (fromElement != null && fromElement is NMElement)
            {
                getelement.windowId = ((NMElement)fromElement).message.windowId;
                getelement.tabid = ((NMElement)fromElement).message.tabid;
                getelement.frameId = ((NMElement)fromElement).message.frameId;
            }
            subresult = NMHook.sendMessageResult(getelement, PluginConfig.protocol_timeout);
            if (subresult != null)
                if (subresult.results != null)
                    foreach (var b in subresult.results)
                    {
                        if (b.cssPath == "true" || b.xPath == "true")
                        {
                            if (results.Count > maxresults) continue;
                            if (fromNMElement != null)
                            {
                                //b.uix = fromNMElement.X + b.x;
                                //b.uiy = fromNMElement.Y + b.y;
                            }
                            var nme = new NMElement(b);
                            results.Add(nme);
                        }
                    }
            return results.ToArray();
        }
    }
}
