#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Chiki.Client.Flow;
using Chiki.Client.Profiles;
using Chiki.Client.Screens;
using Chiki.Client.Visuals;
using Chiki.Sim;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Client
{
    /// <summary>The menus in the game's skin and font (P10.1) and the title's logo (P10.2).</summary>
    public class Screens
    {
        private string _root = null!;
        private readonly List<GameObject> _hosts = new List<GameObject>();

        [SetUp]
        public void FreshRoot()
        {
            _root = Path.Combine(Application.temporaryCachePath, "chiki-tests", "screens-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void RemoveRoot()
        {
            foreach (var host in _hosts)
            {
                if (host != null)
                {
                    Object.DestroyImmediate(host);
                }
            }

            _hosts.Clear();
            ActiveProfile.Clear();
            ClientTestContent.ClearCatalogues();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator every_screen_uses_skin_and_font()
        {
            var catalogue = ClientTestContent.ShippedVisuals();
            ClientTestContent.ShippedAudio();
            var flow = CalibratedFlow("skin");
            yield return null;

            var checkedScreens = new List<string>();
            void Check(string screen, Component? root)
            {
                Assert.That(root, Is.Not.Null, "the " + screen + " screen did not open");
                AssertSkinned(catalogue, screen, root!.gameObject);
                checkedScreens.Add(screen);
            }

            Check("pre-run", flow.PreRun);

            flow.OpenSettings();
            Check("settings", flow.Settings);
            Assert.That(flow.Settings!.ChooseClose(), Is.True);
            yield return null;

            flow.OpenCalibration();
            Check("calibration", flow.Calibration);
            flow.Calibration!.Close();
            yield return null;

            Assert.That(flow.TryStartRun("chiki-1"), Is.EqualTo(StartRunResult.Started));
            flow.EnterMap();
            yield return null;
            Check("map", flow.Map);

            yield return ClientTestContent.OpenBattleNode(flow);
            Check("pre-battle", flow.PreBattle);
            Assert.That(flow.PreBattle!.ChooseEdit(), Is.True);
            yield return null;
            Check("Binder", flow.Binder);
            Assert.That(flow.Binder!.ChooseBack(), Is.True);
            yield return null;

            var run = flow.Run!;
            flow.SettleBattle(ClientTestContent.FightNodeBattle(run));
            yield return null;
            Check("reward", flow.Reward);
            flow.SkipReward();
            yield return null;

            var host = new GameObject("screens-direct");
            _hosts.Add(host);
            var stopNode = run.CurrentMap.Nodes.First(n => !n.IsBattle);
            Check("stop", StopPanel.Build(host.transform, stopNode));
            Check("run-end", RunEndScreen.Build(host.transform, new RunEndSummary(RunStatus.Died, "chiki-1", 1, 0, 10, 3, Array.Empty<RunEndUnlock>())));

            Assert.That(checkedScreens, Has.Count.EqualTo(9));
            Assert.That(catalogue.Missing, Is.Empty, "a screen fell back: " + string.Join(", ", catalogue.Missing));
        }

        [UnityTest]
        [Timeout(30000)]
        public IEnumerator prerun_shows_logo()
        {
            var catalogue = ClientTestContent.ShippedVisuals();
            var flow = CalibratedFlow("logo");
            yield return null;

            var screen = flow.PreRun;
            Assert.That(screen, Is.Not.Null, "the pre-run screen did not open");
            Assert.That(screen!.Logo, Is.Not.Null, "the pre-run screen shows no logo");
            Assert.That(screen.Logo!.gameObject.activeInHierarchy, Is.True);
            Assert.That(screen.Logo.sprite, Is.SameAs(catalogue.Sprite(PreRunScreen.LogoKind, PreRunScreen.LogoId)), "the logo is not the catalogue's");
            Assert.That(screen.Title, Is.Null, "the title text still shows beside the logo");
            Assert.That(screen.Background.gameObject.activeInHierarchy, Is.True);
            Assert.That(screen.Background.sprite, Is.SameAs(catalogue.Sprite(PreRunScreen.BackgroundKind, PreRunScreen.BackgroundId)), "the title background is not the catalogue's");
            Assert.That(catalogue.Missing, Is.Empty, "the pre-run screen fell back: " + string.Join(", ", catalogue.Missing));

            Assert.That(screen.ChooseSettings(), Is.True, "Settings did not accept input");
            yield return null;
            Assert.That(flow.Settings, Is.Not.Null, "Settings did not open");
            flow.Settings!.ChooseClose();
            yield return null;

            Assert.That(screen.ChooseStartRun(), Is.True, "Start Run did not accept input");
            yield return null;
            Assert.That(flow.Map, Is.Not.Null, "Start Run did not start a run");
        }

        private GameFlow CalibratedFlow(string name)
        {
            var store = new ProfileStore(_root);
            var profile = store.Create(name);
            profile.Calibrated = true;
            store.Save(profile);
            var host = new GameObject("flow-" + name);
            _hosts.Add(host);
            var flow = host.AddComponent<GameFlow>();
            flow.Begin(profile, store);
            return flow;
        }

        /// <summary>Every text under a screen in the shipped font; every button with the four skin states; every toggle on the skin's box and check.</summary>
        private static void AssertSkinned(VisualCatalogue catalogue, string screen, GameObject root)
        {
            var regular = catalogue.TextFont(false);
            var bold = catalogue.TextFont(true);
            Assert.That(regular, Is.Not.Null, "the shipped catalogue holds no regular font");
            Assert.That(bold, Is.Not.Null.And.Not.SameAs(regular), "the shipped catalogue holds no bold font");

            var texts = root.GetComponentsInChildren<Text>(true);
            Assert.That(texts, Is.Not.Empty, "the " + screen + " screen has no text");
            foreach (var text in texts)
            {
                Assert.That(text.font == regular || text.font == bold, Is.True, "'" + text.text + "' on the " + screen + " screen is set in " + (text.font != null ? text.font.name : "no font") + ", not the shipped font");
            }

            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                string name = "the " + screen + " screen's " + button.name + " button";
                Assert.That(button.transition, Is.EqualTo(Selectable.Transition.SpriteSwap), name + " does not swap sprites");
                Assert.That(((Image)button.targetGraphic).sprite, Is.SameAs(catalogue.Sprite(UiSkin.Kind, UiSkin.ButtonNormalId)), name + " has no normal sprite");
                Assert.That(button.spriteState.highlightedSprite, Is.SameAs(catalogue.Sprite(UiSkin.Kind, UiSkin.ButtonHighlightedId)), name + " has no highlighted sprite");
                Assert.That(button.spriteState.pressedSprite, Is.SameAs(catalogue.Sprite(UiSkin.Kind, UiSkin.ButtonPressedId)), name + " has no pressed sprite");
                Assert.That(button.spriteState.disabledSprite, Is.SameAs(catalogue.Sprite(UiSkin.Kind, UiSkin.ButtonDisabledId)), name + " has no disabled sprite");
            }

            foreach (var toggle in root.GetComponentsInChildren<Toggle>(true))
            {
                Assert.That(((Image)toggle.targetGraphic).sprite, Is.SameAs(catalogue.Sprite(UiSkin.Kind, UiSkin.ToggleBoxId)), "the " + screen + " screen's toggle has no box sprite");
                Assert.That(((Image)toggle.graphic).sprite, Is.SameAs(catalogue.Sprite(UiSkin.Kind, UiSkin.ToggleCheckId)), "the " + screen + " screen's toggle has no check sprite");
            }
        }
    }
}
