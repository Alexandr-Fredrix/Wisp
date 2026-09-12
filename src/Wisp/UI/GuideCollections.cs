using System;
using System.Linq;
using UnityEngine;
using Wisp.Core;

namespace Wisp.UI
{
    public sealed partial class GuideWindow
    {
        private int collectionGroup, collectionItem, collectionMap, collectionGroupPage, collectionItemPage;
        private bool revealCollectionGroup = true, revealCollectionItem = true;
        private bool expandedCollectionMap, collectionInfo;
        private Vector2 collectionScroll;
        private string[] collectionGroupLabels, collectionItemLabels;
        private int labelledCollectionGroup = -1;
        private bool collectionLabelEnglish;
        private CollectionItem CurrentCollectionItem
        {
            get { var items = media.Collections[collectionGroup].Items; return items.Length == 0 ? null : items[Mathf.Clamp(collectionItem, 0, items.Length - 1)]; }
        }
        private int CollectionMapIndex
        {
            get { int count = CurrentCollectionItem.Maps.Length; return count == 0 ? 0 : (collectionMap % count + count) % count; }
        }
        private void OpenCollection(string id)
        {
            int index = Array.FindIndex(media.Collections, g => g.Id == id);
            if (index < 0) return;
            ChangeTab(4); SelectCollectionGroup(index); padPane = 1;
        }
        private void SelectCollectionGroup(int index)
        {
            collectionGroup = Mathf.Clamp(index, 0, media.Collections.Length - 1);
            collectionItem = collectionMap = 0; collectionScroll = mapPan = Vector2.zero; mapZoom = 1;
            revealCollectionGroup = revealCollectionItem = true; collectionInfo = false;
        }
        private void SelectCollectionItem(int index)
        {
            collectionItem = Mathf.Clamp(index, 0, media.Collections[collectionGroup].Items.Length - 1);
            collectionMap = 0; collectionScroll = mapPan = Vector2.zero; mapZoom = 1;
            revealCollectionItem = true; collectionInfo = false;
        }
        private void NextCollectionMap(int direction)
        { collectionMap += direction; mapPan = Vector2.zero; mapZoom = 1; }
        private void UpdateCollections(InControl.InputDevice device, int dx, int dy, bool retriedImage)
        {
            if (collectionInfo)
            {
                collectionScroll.y = Mathf.Max(0, collectionScroll.y + dy * 60 - device.RightStickY.Value * Time.unscaledDeltaTime * 350);
                return;
            }
            if (!contentFocus)
            {
                if (dx != 0) padPane = Mathf.Clamp(padPane + dx, 0, 2);
                else if (dy != 0 && padPane == 0) SelectCollectionGroup(collectionGroup + dy);
                else if (dy != 0 && padPane == 1) SelectCollectionItem(collectionItem + dy);
                if (device.Action1.WasPressed) { if (padPane < 2) padPane++; else contentFocus = true; }
                if (padPane > 0) collectionScroll.y = Mathf.Max(0, collectionScroll.y - device.RightStickY.Value * Time.unscaledDeltaTime * 350 + (padPane == 2 ? dy * 60 : 0));
                if (device.Action4.WasPressed) NextCollectionMap(1);
            }
            else
            {
                if (device.DPadLeft.WasPressed) NextCollectionMap(-1);
                if (device.DPadRight.WasPressed) NextCollectionMap(1);
            }
            if (device.RightStickButton.WasPressed && padPane > 0) { expandedCollectionMap = !expandedCollectionMap; contentFocus = true; }
            if (device.Action3.WasPressed && !retriedImage) { collectionInfo = true; contentFocus = false; collectionScroll = Vector2.zero; }
        }
        private string CollectionHint()
        {
            if (collectionInfo) return I18n.T("↑↓ / RS: текст · B: к коллекции");
            if (contentFocus) return I18n.T("Стики: карта · LT/RT: масштаб · ←→: место · RS: развернуть · X: повтор / справка · Y: вписать · B: назад");
            return I18n.T("↑↓: выбор · ←→ / A: панель · RS: текст / развернуть · Y: другое место · X: повтор / справка · B: назад");
        }
        private void DrawCollections()
        {
            if (media.Collections.Length == 0) return;
            var group = media.Collections[collectionGroup]; var item = CurrentCollectionItem;
            if (collectionGroupLabels == null || collectionLabelEnglish != I18n.English || labelledCollectionGroup != collectionGroup)
            {
                collectionLabelEnglish = I18n.English; labelledCollectionGroup = collectionGroup;
                collectionGroupLabels = media.Collections.Select(g => I18n.T(g.Title) + "\n" + g.Items.Length).ToArray();
                collectionItemLabels = group.Items.Select(i => I18n.T(i.Title)).ToArray();
            }
            if (expandedCollectionMap && item != null && item.Maps.Length > 0)
            {
                if (Choose(ContentRect(0, 0, 180, 34), I18n.T("‹ К коллекции"))) { expandedCollectionMap = false; contentFocus = false; }
                GUI.Label(ContentRect(190, 0, 910, 38), I18n.T(item.Title), heading);
                DrawCollectionMap(ContentRect(0, 95, 1112, 422), ContentRect(0, 51, 1112, 34), item);
                return;
            }
            GUI.Label(ContentRect(0, 0, 185, 34), I18n.T("Коллекции"), columnHeading);
            GUI.Label(ContentRect(204, 0, 270, 34), I18n.T(group.Title), columnHeading);
            GUI.Label(ContentRect(500, 0, 375, 34), "Hollow Knight Wiki / Team Cherry", small);
            if (Choose(ContentRect(890, 0, 222, 34), I18n.T("О разделе · X"), collectionInfo)) { collectionInfo = !collectionInfo; collectionScroll = Vector2.zero; }
            PagedList(ContentRect(0, 54, 185, 476), collectionGroupLabels, collectionGroup, ref collectionGroupPage, ref revealCollectionGroup, padPane == 0 && !contentFocus,
                i => { SelectCollectionGroup(i); contentFocus = false; padPane = 0; });
            if (labelledCollectionGroup != collectionGroup)
            {
                labelledCollectionGroup = collectionGroup;
                collectionItemLabels = media.Collections[collectionGroup].Items.Select(i => I18n.T(i.Title)).ToArray();
            }
            PagedList(ContentRect(204, 54, 270, 476), collectionItemLabels, collectionItem, ref collectionItemPage, ref revealCollectionItem, padPane == 1 && !contentFocus,
                i => { SelectCollectionItem(i); contentFocus = false; padPane = 1; });
            // Selection can change during this IMGUI event; reacquire the selected data.
            group = media.Collections[collectionGroup]; item = CurrentCollectionItem;
            if (item == null) return;
            if (collectionInfo)
            {
                string summary = I18n.T(group.Summary);
                float height = text.CalcHeight(new GUIContent(summary), ContentRect(0, 0, 578, 0).width);
                collectionScroll = GUI.BeginScrollView(ContentRect(500, 54, 612, 464), collectionScroll, ContentRect(0, 0, 588, height + 24));
                GUI.Label(ContentRect(0, 0, 578, height), summary, text); GUI.EndScrollView(); return;
            }
            DrawCollectionIcon(ContentRect(500, 54, 80, 80), item);
            GUI.Label(ContentRect(595, 54, 507, 85), I18n.T(item.Title), heading);
            string body = (item.Effect.Length > 0 ? I18n.T("Действие амулета") + "\n" + I18n.T(item.Effect) + "\n\n" : "") + I18n.T("Где искать") + "\n" + I18n.T(item.Location);
            float bodyHeight = text.CalcHeight(new GUIContent(body), ContentRect(0, 0, 578, 0).width);
            float descriptionHeight = Mathf.Clamp(bodyHeight + 10, 65, item.Effect.Length > 0 ? 125 : 90);
            collectionScroll = GUI.BeginScrollView(ContentRect(500, 145, 612, descriptionHeight), collectionScroll, ContentRect(0, 0, 588, bodyHeight + 10));
            GUI.Label(ContentRect(0, 0, 578, bodyHeight), body, text); GUI.EndScrollView();
            float toolbarY = 145 + descriptionHeight + 12;
            DrawCollectionMap(ContentRect(500, toolbarY + 44, 612, 518 - toolbarY - 44), ContentRect(500, toolbarY, 612, 32), item);
            if (padPane == 2) FocusCorners(ContentRect(496, toolbarY - 4, 616, 530 - toolbarY));
        }
        private void DrawCollectionMap(Rect viewport, Rect controls, CollectionItem item)
        {
            if (item.Maps.Length == 0) return;
            float x = controls.x, y = controls.y;
            if (item.Maps.Length > 1)
            {
                Border(new Rect(x, y, 144, 32), new Color(.12f,.20f,.25f));
                if (MapButton(new Rect(x, y, 32, 32), "‹")) NextCollectionMap(-1);
                GUI.Label(new Rect(x + 32, y, 80, 32), (CollectionMapIndex + 1) + " / " + item.Maps.Length, pageStyle);
                if (MapButton(new Rect(x + 112, y, 32, 32), "›")) NextCollectionMap(1);
                x += 156;
            }
            Border(new Rect(x, y, 156, 32), new Color(.12f,.20f,.25f));
            if (MapButton(new Rect(x, y, 32, 32), "−")) mapZoom = Mathf.Max(1, mapZoom / 1.4f);
            if (MapButton(new Rect(x + 32, y, 92, 32), Mathf.RoundToInt(mapZoom * 100) + "%")) { mapZoom = 1; mapPan = Vector2.zero; }
            if (MapButton(new Rect(x + 124, y, 32, 32), "+")) mapZoom = Mathf.Min(5, mapZoom * 1.4f);
            var expand = new Rect(controls.xMax - 210, y, 210, 32);
            Border(expand, new Color(.12f,.20f,.25f));
            if (MapButton(expand, I18n.T(expandedCollectionMap ? "Свернуть · RS" : "Развернуть · RS"))) { expandedCollectionMap = !expandedCollectionMap; padPane = 2; contentFocus = expandedCollectionMap; }
            DrawMapTexture(new Rect(viewport.x, viewport.y, viewport.width, viewport.height - 28), item.Maps[CollectionMapIndex].Url);
            GUI.Label(new Rect(viewport.x, viewport.yMax - 26, viewport.width, 26), I18n.T(item.Maps[CollectionMapIndex].Label) + (expandedCollectionMap ? " · Hollow Knight Wiki / Team Cherry" : ""), small);
        }
        private void DrawCollectionIcon(Rect rect, CollectionItem item)
        {
            if (item.CharmId > 0 && CharmIconList.Instance != null)
            {
                var sprite = CharmIconList.Instance.GetSprite(item.CharmId);
                if (sprite != null)
                {
                    var texture = sprite.texture; var source = sprite.textureRect;
                    float scale = Mathf.Min(rect.width / source.width, rect.height / source.height);
                    var target = new Rect(rect.center.x - source.width * scale / 2, rect.center.y - source.height * scale / 2, source.width * scale, source.height * scale);
                    GUI.DrawTextureWithTexCoords(target, texture, new Rect(source.x / texture.width, source.y / texture.height, source.width / texture.width, source.height / texture.height));
                    return;
                }
            }
            var icon = media.Get(item.Icon);
            if (icon != null) GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
            else if (item.Icon.Length > 0) DrawImageStatus(rect, item.Icon);
        }
    }
}
