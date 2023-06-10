using CatPunchPunchDP.Modules;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DyeableRect = Menu.Remix.MixedUI.DyeableRect;

public class PunchConfig : OptionInterface
{
    static bool inited;

    public static Dictionary<PunchType, ConfigSetting> configSettings = new Dictionary<PunchType, ConfigSetting>();
    static Dictionary<PunchType, Configurable<int>> coolDownConfigs = new Dictionary<PunchType, Configurable<int>>();

    static Dictionary<PunchType, Configurable<float>> floatValConfigs = new Dictionary<PunchType, Configurable<float>>();
    static Dictionary<PunchType, Configurable<int>> intValConfigs = new Dictionary<PunchType, Configurable<int>>();
    static Dictionary<PunchType, Configurable<string>> stringValConfig = new Dictionary<PunchType, Configurable<string>>();

    public Dictionary<PunchType, PunchInfoBox> infoBoxes = new Dictionary<PunchType, PunchInfoBox>();

    public PunchConfig()
    {
        if (!inited)
        {
            inited = true;
            SetUpConfig();
        }
    }

    public static Configurable<int> GetCoolDownConfig(PunchType punchType) => coolDownConfigs[punchType];
    public static Configurable<float> GetFloatValConfig(PunchType punchType)
    {
        if (floatValConfigs.ContainsKey(punchType))
            return floatValConfigs[punchType];
        else
            return GetFloatValConfig(PunchType.NormalPunch);
    }
    public static Configurable<int> GetIntValConfig(PunchType punchType) => intValConfigs[punchType];
    public static Configurable<string> GetStringConfig(PunchType punchType) => stringValConfig[punchType];

    public override void Initialize()
    {
        base.Initialize();
        infoBoxes.Clear();

        Tabs = new OpTab[1];
        Tabs[0] = new OpTab(this, "Main");
        OpLabel title = new OpLabel(new Vector2(300 - 100, 550), new Vector2(200, 30), "CatPunchPunch", FLabelAlignment.Center, true) { color = MenuColorEffect.rgbWhite};
        OpScrollBox opScrollBox = new OpScrollBox(new Vector2(50, 50), new Vector2(500, 450), (ExtEnum<PunchType>.values.entries.Count + 1) * 125f + 100f);
        Tabs[0].AddItems(opScrollBox,title);

        int index = 0;
        foreach (var punch in ExtEnum<PunchType>.values.entries)
        {
            var punchType = new PunchType(punch);
            var setting = GetConfigSetting(punchType);
            
            PunchInfoBox punchInfoBox;
            if (setting.isIntData)
                punchInfoBox = new PunchInfo_Int(opScrollBox, punchType, new Vector2(0, opScrollBox.contentSize - (index + 1) * 125f - 10f));
            else
                punchInfoBox = new PunchInfo_Float(opScrollBox, punchType, new Vector2(0, opScrollBox.contentSize - (index + 1) * 125f - 10f));

            infoBoxes.Add(new PunchType(punch), punchInfoBox);
            punchInfoBox.MakeUIElements();
            index++;
        }
    }

    public void SetUpConfig()
    {
        foreach (var punch in ExtEnum<PunchType>.values.entries)
        {
            var punchType = new PunchType(punch);
            var setting = GetConfigSetting(punchType);
            try
            {
                configSettings.Add(punchType, setting);

                coolDownConfigs.Add(punchType, config.Bind($"{punch}_coolDown", setting.defaultCoolDown, new ConfigAcceptableRange<int>(setting.coolDownLow, setting.coolDownHigh)));

                if (setting.isIntData)
                    intValConfigs.Add(punchType, config.Bind($"{punch}_intVal", setting.defaultIntVal, new ConfigAcceptableRange<int>(setting.intValLow, setting.intValHigh)));
                else
                {
                    var range = new ConfigAcceptableRange<float>(setting.floatValLow, setting.floatValHigh);
                    floatValConfigs.Add(punchType, config.Bind($"{punch}_floatVal", setting.defaultFloatVal, range));
                }

                if (setting.hasStringVal)
                {
                    stringValConfig.Add(punchType, config.Bind<string>($"{punch}_stringVal", setting.defaultStringVal));
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PunchPlugin.Log($"Exception happend when load {punch} config\n{setting}");
            }
        }
    }

    public static ConfigSetting GetConfigSetting(PunchType punchType)
    {
        if (punchType == PunchType.FastPunch)
            return new ConfigSetting()
            {
                elementName = ItemSymbol.SpriteNameForItem(AbstractPhysicalObject.AbstractObjectType.Rock, 0),
                color = ItemSymbol.ColorForItem(AbstractPhysicalObject.AbstractObjectType.Rock, 0),

                defaultCoolDown = 5,
                coolDownHigh = 10,
                coolDownLow = 1,

                valName = "Damage",
                defaultFloatVal = 0.1f,
                floatValHigh = 2f,
                floatValLow = 0.05f,
            };
        else if (punchType == PunchType.BombPunch)
            return new ConfigSetting()
            {
                elementName = ItemSymbol.SpriteNameForItem(AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, 0),
                color = ItemSymbol.ColorForItem(AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, 0),

                defaultCoolDown = 60,
                coolDownHigh = 120,
                coolDownLow = 20,

                valName = "Damage",
                defaultFloatVal = 3f,
                floatValLow = 1f,
                floatValHigh = 10f
            };
        else if (punchType == PunchType.FriendlyPunch)
            return new ConfigSetting()
            {
                elementName = ItemSymbol.SpriteNameForItem(AbstractPhysicalObject.AbstractObjectType.DataPearl, 0),

                defaultCoolDown = 80,
                coolDownHigh = 120,
                coolDownLow = 40,

                valName = "Heal&Like",
                defaultFloatVal = 0.1f,
                floatValLow = 0.05f,
                floatValHigh = 0.3f
            };
        else if (punchType == PunchType.FriendlyPunch_HighLevel)
            return new ConfigSetting()
            {
                elementName = ItemSymbol.SpriteNameForItem(AbstractPhysicalObject.AbstractObjectType.DataPearl, 1),
                color = ItemSymbol.ColorForItem(AbstractPhysicalObject.AbstractObjectType.DataPearl, 1),

                defaultCoolDown = 60,
                coolDownHigh = 120,
                coolDownLow = 20,

                valName = "Heal&Like",
                defaultFloatVal = 0.5f,
                floatValLow = 0.3f,
                floatValHigh = 1f
            };
        else if (punchType == PunchType.NeedlePunch)
            return new ConfigSetting()
            {
                elementName = ItemSymbol.SpriteNameForItem(AbstractPhysicalObject.AbstractObjectType.NeedleEgg, 0),
                color = ItemSymbol.ColorForItem(AbstractPhysicalObject.AbstractObjectType.NeedleEgg, 0),
                isIntData = true,

                defaultCoolDown = 200,
                coolDownHigh = 400,
                coolDownLow = 40,

                valName = "NeedleArmy",
                defaultIntVal = 1,
                intValHigh = 5,
                intValLow = 1
            };
        else if (punchType == PunchType.LaserPunch)
            return new ConfigSetting()
            {
                elementName = CreatureSymbol.SpriteNameOfCreature(new IconSymbol.IconSymbolData(CreatureTemplate.Type.VultureGrub, AbstractPhysicalObject.AbstractObjectType.Creature, 0)),
                color = CreatureSymbol.ColorOfCreature(new IconSymbol.IconSymbolData(CreatureTemplate.Type.VultureGrub,AbstractPhysicalObject.AbstractObjectType.Creature,0)),

                defaultCoolDown = 60,
                coolDownHigh = 120,
                coolDownLow = 10,

                valName = "Damage",
                defaultFloatVal = 1f,
                floatValLow = 0.5f,
                floatValHigh = 5f,

                stringValName = "laser punch type",
                hasStringVal = true,
                defaultStringVal = "Blast",
                stringValOptions = new string[] { "Blast", "Burning" }
            };
        else if (punchType == PunchType.BeePunch)
            return new ConfigSetting()
            {
                elementName = ItemSymbol.SpriteNameForItem(AbstractPhysicalObject.AbstractObjectType.SporePlant, 0),
                color = ItemSymbol.ColorForItem(AbstractPhysicalObject.AbstractObjectType.SporePlant, 0),
                isIntData = true,

                defaultCoolDown = 60,
                coolDownHigh = 120,
                coolDownLow = 10,

                valName = "BeeArmy",
                defaultIntVal = 4,
                intValHigh = 10,
                intValLow = 2
            };
        else if (punchType == PunchType.BlackHolePunch)
            return new ConfigSetting()
            {
                elementName = ItemSymbol.SpriteNameForItem(MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType.SingularityBomb, 0),
                color = ItemSymbol.ColorForItem(MoreSlugcats.MoreSlugcatsEnums.AbstractObjectType.SingularityBomb, 0),
                isIntData = true,

                defaultCoolDown = 300,
                coolDownHigh = 400,
                coolDownLow = 100,

                valName = "LifeTime",
                defaultIntVal = 300,
                intValHigh = 600,
                intValLow = 100
            };
        else if (punchType == PunchType.PuffPunch)
            return new ConfigSetting()
            {
                elementName = ItemSymbol.SpriteNameForItem(AbstractPhysicalObject.AbstractObjectType.PuffBall, 0),
                color = ItemSymbol.ColorForItem(AbstractPhysicalObject.AbstractObjectType.PuffBall, 0),
                isIntData = true,

                defaultCoolDown = 100,
                coolDownLow = 40,
                coolDownHigh = 140,

                valName = "PuffCloud",
                defaultIntVal = 40,
                intValLow = 10,
                intValHigh = 70,
            };
        else
            return new ConfigSetting()
            {
                elementName = "",
                color = new Color(1f, 1f, 1f, 0f),

                defaultCoolDown = 10,
                coolDownHigh = 20,
                coolDownLow = 6,

                valName = "Damage",
                defaultFloatVal = 0.1f,
                floatValHigh = 2f,
                floatValLow = 0.05f
            };
    }


    public class PunchInfoBox
    {
        static FieldInfo OnClickField = typeof(OpSimpleButton).GetField("OnClick", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

        public OpScrollBox scrollBox;
        public PunchType punchType;

        public Vector2 pos;
        
        public ConfigSetting Setting => configSettings[punchType];

        protected OpSlider coolDownSlider;
        protected OpSwitchButton stringValButton;

        public PunchInfoBox(OpScrollBox scrollBox, PunchType punchType, Vector2 pos)
        {
            this.scrollBox = scrollBox;
            this.pos = pos;
            this.punchType = punchType;
        }

        public virtual void MakeUIElements()
        {
            OpRect opRect = new OpRect(pos + Vector2.one * 10f, new Vector2(500f - 40f, 115));
            OpLabel title = new OpLabel(pos.x + 20f, pos.y + 60f + 35f, punchType.ToString(), true) { color = MenuColorEffect.rgbWhite };

            pos.x += 20f;

            var opButton_Reset = new OpSimpleButton(new Vector2(pos.x + 10 + 60 + 250 + 85f, pos.y + 20f), new Vector2(35f, 20f), "Reset");
            coolDownSlider = new OpSlider(GetCoolDownConfig(punchType), pos + Vector2.one * 10 + Vector2.right * 130 + Vector2.up * 60f, 200);

            var title_break = new OpLabel(pos.x + 30f, pos.y + 60f + 15f, "CoolDown");
            var title_coolDownLow = new OpLabel(pos.x + 10f + 110f - 15f, pos.y + 15f + 60f, coolDownSlider.min.ToString());
            var title_coolDownHigh = new OpLabel(pos.x + 10f + 130f + 200f + 15f, pos.y + 15f + 60f, coolDownSlider.max.ToString());

            scrollBox.AddItems(opRect, title, opButton_Reset, coolDownSlider, title_break, title_coolDownLow, title_coolDownHigh);

            if (Setting.elementName != "")
            {
                OpImage opImage = new OpImage(pos + Vector2.up * 20, Setting.elementName)
                {
                    color = Setting.color
                };
                scrollBox.AddItems(opImage);
            }

            if (Setting.hasStringVal)
            {
                stringValButton = new OpSwitchButton(GetStringConfig(punchType), new Vector2(pos.x + 10 + 60 + 250 + 85f - 80f - 15f, pos.y + 20f), new Vector2(80f, 20f), Setting.stringValOptions)
                {
                    description = $"Click to change value of {Setting.stringValName}"
                };
                scrollBox.AddItems(stringValButton);
            }

            OnClickField.GetValue(opButton_Reset);

            var OnClickHandle = OnClickField.GetValue(opButton_Reset) as OnSignalHandler;
            OnClickHandle += Reset;
            OnClickField.SetValue(opButton_Reset, OnClickHandle);
        }

        public virtual void Reset(UIfocusable trigger)
        {
            coolDownSlider.value = coolDownSlider.defaultValue;
            if(stringValButton != null)
                stringValButton.value = stringValButton.defaultValue;
        }
    }

    public class PunchInfo_Float : PunchInfoBox
    {
        protected OpFloatSlider floatValSlider;
        public PunchInfo_Float(OpScrollBox scrollBox, PunchType punchType, Vector2 pos) : base(scrollBox, punchType, pos)
        {
        }

        public override void MakeUIElements()
        {
            base.MakeUIElements();

            floatValSlider = new OpFloatSlider(GetFloatValConfig(punchType), pos + Vector2.one * 10 + Vector2.right * 130f + Vector2.up * 30f, 200, 2);
            floatValSlider.Increment = 1;

            var title_floatVal = new OpLabel(pos.x + 30f, pos.y + 30f + 15f, Setting.valName);
            var title_floatValLow = new OpLabel(pos.x + 10f + 110f - 15f, pos.y + 15f + 30f, floatValSlider.min.ToString());
            var title_floatValHigh = new OpLabel(pos.x + 10f + 130f + 200f + 15f, pos.y + 15f + 30f, floatValSlider.max.ToString());

            scrollBox.AddItems(floatValSlider, title_floatVal, title_floatValLow, title_floatValHigh);
        }

        public override void Reset(UIfocusable trigger)
        {
            base.Reset(trigger);
            floatValSlider.value = floatValSlider.defaultValue;
        }
    }

    public class PunchInfo_Int : PunchInfoBox
    {
        protected OpSlider intValSlider;
        public PunchInfo_Int(OpScrollBox scrollBox, PunchType punchType, Vector2 pos) : base(scrollBox, punchType, pos)
        {
        }

        public override void MakeUIElements()
        {
            base.MakeUIElements();

            intValSlider = new OpSlider(GetIntValConfig(punchType), pos + Vector2.one * 10 + Vector2.right * 130f + Vector2.up * 30f, 200);
            var title_intVal = new OpLabel(pos.x + 30f, pos.y + 30f + 15f, Setting.valName);
            var title_intValLow = new OpLabel(pos.x + 10f + 110f - 15f, pos.y + 15f + 30f, intValSlider.min.ToString());
            var title_intValHigh = new OpLabel(pos.x + 10f + 130f + 200f + 15f, pos.y + 15f + 30f, intValSlider.max.ToString());

            scrollBox.AddItems(intValSlider, title_intVal, title_intValLow, title_intValHigh);
        }

        public override void Reset(UIfocusable trigger)
        {
            base.Reset(trigger);
            intValSlider.value = intValSlider.defaultValue;
        }
    }

    public class ConfigSetting
    {
        public string elementName;
        public bool isIntData;
        public Color color = Color.white;

        public int defaultCoolDown;
        public int coolDownLow;
        public int coolDownHigh;

        public string valName;

        public float defaultFloatVal;
        public float floatValLow;
        public float floatValHigh;

        public int defaultIntVal;
        public int intValLow;
        public int intValHigh;

        public bool hasStringVal;
        public string defaultStringVal;
        public string stringValName;
        public string[] stringValOptions;

        public override string ToString()
        {
            return $"elementName : {elementName}\nColor : {color}\nisIntData : {isIntData}\nCoolDown : {defaultCoolDown} - {coolDownLow}=={coolDownHigh}\n{valName} : {(isIntData ? defaultIntVal : defaultFloatVal)} - {(isIntData ? intValLow : floatValLow)}=={(isIntData ? intValLow : floatValHigh)}";
        }
    }

    public class OpSwitchButton : UIconfig
    {
        protected int _heldCounter;

        public SoundID soundClick = SoundID.MENU_Button_Standard_Button_Pressed;

        private FLabelAlignment _alignment;
        private string _text;
        public Color colorEdge = MenuColorEffect.rgbMediumGrey;
        public Color colorFill = MenuColorEffect.rgbBlack;

        protected readonly FLabel _label;
        protected readonly DyeableRect _rect;
        protected readonly DyeableRect _rectH;

        int valueIndex = -1;
        string[] options;

        public OpSwitchButton(ConfigurableBase config, Vector2 pos,Vector2 size, string[] options) : base(config,pos, new Vector2(24f, 24f))
        {
            this.OnPressInit += base.FocusMoveDisallow;
            this.OnClick += base.FocusMoveDisallow;
            this.OnPressHold += base.FocusMoveDisallow;
            this._size = new Vector2(Mathf.Max(24f, size.x), Mathf.Max(24f, size.y));
            this._rect = new DyeableRect(this.myContainer, Vector2.zero, base.size, true);
            this._rectH = new DyeableRect(this.myContainer, Vector2.zero, base.size, false);

            this._label = UIelement.FLabelCreate(value, false);
            this.text = value;
            this.myContainer.AddChild(this._label);
            UIelement.FLabelPlaceAtCenter(this._label, Vector2.zero, base.size);

            this.options = options;
            CheckIndex();

            OnClick += OpSwitchButton_OnClick;
            PunchPlugin.Log($"OnSwitchButtonClick : orig index{valueIndex}");
        }

        private void OpSwitchButton_OnClick(UIfocusable trigger)
        {
            PunchPlugin.Log($"OnSwitchButtonClick : orig index{valueIndex},orig option {options[valueIndex]}, orig value {value}");

            if (trigger != this)
                return;
            valueIndex++;
            if (valueIndex >= options.Length)
                valueIndex = 0;

            value = options[valueIndex];
            text = value;
            PunchPlugin.Log($"OnSwitchButtonClick : next index{valueIndex},next option {options[valueIndex]}, next value {value}");
        }

        public override string DisplayDescription()
        {
            if (!string.IsNullOrEmpty(this.description))
            {
                return this.description;
            }
            return OptionalText.GetText(base.MenuMouseMode ? OptionalText.ID.OpSimpleButton_MouseTuto : OptionalText.ID.OpSimpleButton_NonMouseTuto);
        }

        public FLabelAlignment alignment
        {
            get
            {
                return this._alignment;
            }
            set
            {
                if (this._alignment != value)
                {
                    this._alignment = value;
                    this.Change();
                }
            }
        }

        public string text
        {
            get => _text;
            set
            {
                if (_text == value)
                    return;
                _text = value;
                _label.text = LabelTest.TrimText(this._text, base.size.x, true, false);
                Change();
            }
        }

        public override void Change()
        {
            _size = new Vector2(Mathf.Max(24f, size.x), Mathf.Max(24f, size.y));
            base.Change();
            UIelement.FLabelPlaceAtCenter(_label, Vector2.zero, size);
            if (alignment != FLabelAlignment.Center)
            {
                _label.alignment = alignment;
                if (alignment == FLabelAlignment.Right)
                    _label.x = size.x - 5f;
                else
                    _label.x = 5f;
            }
            _rect.size = size;
            _rectH.size = size;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            _rect.GrafUpdate(timeStacker);
            _rectH.GrafUpdate(timeStacker);
            _rect.addSize = new Vector2(6f, 6f) * base.bumpBehav.AddSize;
            if (greyedOut)
            {
                _rect.colorEdge = base.bumpBehav.GetColor(this.colorEdge);
                _rect.colorFill = base.bumpBehav.GetColor(this.colorFill);
                _rectH.Hide();
                return;
            }
            _rectH.Show();
            _rectH.colorEdge = base.bumpBehav.GetColor(this.colorEdge);
            _rectH.addSize = new Vector2(-2f, -2f) * base.bumpBehav.AddSize;
            float alpha = ((Focused || MouseOver) && !held) ? ((0.5f + 0.5f * bumpBehav.Sin(10f)) * bumpBehav.AddSize) : 0f;
            for (int i = 0; i < 8; i++)
            {
                _rectH.sprites[i].alpha = alpha;
            }
            _rect.colorEdge = bumpBehav.GetColor(this.colorEdge);
            _rect.fillAlpha = bumpBehav.FillAlpha;
            _rect.colorFill = colorFill;

            CheckIndex();
        }

        public void CheckIndex()
        {
            int index = 0;
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i].Equals(value))
                    index = i;
            }
            if (index != valueIndex)
            {
                valueIndex = index;
                text = options[index];
            }
        }

        public override void NonMouseSetHeld(bool newHeld)
        {
            base.NonMouseSetHeld(newHeld);
            if (!newHeld)
            {
                this._heldCounter = 0;
                return;
            }
            OnSignalHandler onPressInit = this.OnPressInit;
            if (onPressInit == null)
            {
                return;
            }
            onPressInit(this);
        }

        // Token: 0x06003037 RID: 12343 RVA: 0x003AD05C File Offset: 0x003AB25C
        public override void Update()
        {
            if (greyedOut && held)
            {
                held = false;
                base.PlaySound(soundClick);
                OnSignalHandler onClick = OnClick;
                if (onClick != null)
                {
                    onClick(this);
                }
            }
            base.Update();
            _rect.Update();
            _rectH.Update();
            if (this.greyedOut)
            {
                this._heldCounter = 0;
                return;
            }
            if (base.MenuMouseMode)
            {
                if (this.MouseOver)
                {
                    if (Input.GetMouseButton(0))
                    {
                        if (!this.held)
                        {
                            OnSignalHandler onPressInit = this.OnPressInit;
                            if (onPressInit != null)
                            {
                                onPressInit(this);
                            }
                        }
                        this.held = true;
                        this._heldCounter++;
                    }
                    else if (this.held)
                    {
                        this.held = false;
                        if (this.OnClick != null)
                        {
                            this.OnClick(this);
                            base.PlaySound(this.soundClick);
                        }
                        this._heldCounter = 0;
                    }
                }
                else if (!Input.GetMouseButton(0))
                {
                    this.held = false;
                    this._heldCounter = 0;
                }
            }
            else if (this.held)
            {
                if (base.CtlrInput.jmp)
                {
                    this._heldCounter++;
                }
                else
                {
                    this.held = false;
                    if (this.OnClick != null)
                    {
                        this.OnClick(this);
                        base.PlaySound(this.soundClick);
                    }
                    this._heldCounter = 0;
                }
            }
            if (this.OnPressHold != null && this._heldCounter > ModdingMenu.DASinit && this._heldCounter % ModdingMenu.DASdelay == 1)
            {
                this.OnPressHold(this);
                base.bumpBehav.sin = 0.5f;
            }
        }

        public event OnSignalHandler OnClick;
        public event OnSignalHandler OnPressInit;
        public event OnSignalHandler OnPressHold;
    }
}
