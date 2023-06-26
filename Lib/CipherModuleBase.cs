using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace CompressionCiphersLib
{
    public abstract class CipherModuleBase : MonoBehaviour
    {
        public TextMesh[] ScreenTexts;
        public KMBombInfo Bomb;
        public KMBombModule Module;
        public AudioClip[] Sounds;
        public KMAudio Audio;
        public TextMesh SubmitText;

        public KMSelectable LeftArrow;
        public KMSelectable RightArrow;
        public KMSelectable Submit;
        public KMSelectable[] Keyboard;

        protected readonly string[][] _pages = { new string[3], new string[3] };
        protected string _answer;
        private int _page;
        private bool _submitScreen;
        private bool _moduleSolved;
        private bool _moduleSelected;

        protected abstract string loggingTag { get; }

        void Awake()
        {
            LeftArrow.OnInteract += delegate { left(LeftArrow); return false; };
            RightArrow.OnInteract += delegate { right(RightArrow); return false; };
            Submit.OnInteract += delegate { submitWord(Submit); return false; };
            Module.GetComponent<KMSelectable>().OnFocus += delegate { _moduleSelected = true; };
            Module.GetComponent<KMSelectable>().OnDefocus += delegate { _moduleSelected = false; };
            foreach (var keybutton in Keyboard)
                keybutton.OnInteract += letterPress(keybutton);
            StartCoroutine(updateScreensLater());
        }

        private IEnumerator updateScreensLater()
        {
            yield return null;
            updateScreens();
        }

        void left(KMSelectable arrow)
        {
            if (!_moduleSolved)
            {
                Audio.PlaySoundAtTransform(Sounds[0].name, transform);
                _submitScreen = false;
                arrow.AddInteractionPunch();
                _page = (_page + _pages.Length - 1) % _pages.Length;
                updateScreens();
            }
        }

        void right(KMSelectable arrow)
        {
            if (!_moduleSolved)
            {
                Audio.PlaySoundAtTransform(Sounds[0].name, transform);
                _submitScreen = false;
                arrow.AddInteractionPunch();
                _page = (_page + 1) % _pages.Length;
                updateScreens();
            }
        }

        private void updateScreens()
        {
            SubmitText.text = (_page + 1).ToString();
            for (var screen = 0; screen < 3; screen++)
            {
                ScreenTexts[screen].text = _pages[_page][screen] ?? "";
                ScreenTexts[screen].fontSize = getFontSize(_pages[_page][screen]?.Length);
            }
        }

        private int getFontSize(int? length) => length == null || length.Value < 6 ? 45 : length.Value == 6 ? 40 : length.Value == 7 ? 35 : 30;

        void submitWord(KMSelectable submitButton)
        {
            if (!_moduleSolved)
            {
                submitButton.AddInteractionPunch();
                if (ScreenTexts[2].text.Equals(_answer))
                {
                    Audio.PlaySoundAtTransform(Sounds[2].name, transform);
                    Debug.LogFormat("[{0}] Module solved.", loggingTag);
                    Module.HandlePass();
                    _moduleSolved = true;
                    ScreenTexts[2].text = "";
                }
                else
                {
                    Audio.PlaySoundAtTransform(Sounds[3].name, transform);
                    Debug.LogFormat("[{0}] You submitted {1}. Strike!", loggingTag, ScreenTexts[2].text);
                    Module.HandleStrike();
                    _page = 0;
                    updateScreens();
                    _submitScreen = false;
                }
            }
        }

        private KMSelectable.OnInteractHandler letterPress(KMSelectable pressed)
        {
            return delegate
            {
                if (!_moduleSolved)
                {
                    pressed.AddInteractionPunch(.1f);
                    Audio.PlaySoundAtTransform(Sounds[1].name, transform);
                    if (_submitScreen)
                    {
                        if (ScreenTexts[2].text.Length < 8)
                            ScreenTexts[2].text += pressed.GetComponentInChildren<TextMesh>().text;
                    }
                    else
                    {
                        SubmitText.text = "SUB";
                        ScreenTexts[0].text = "";
                        ScreenTexts[1].text = "";
                        ScreenTexts[2].text = pressed.GetComponentInChildren<TextMesh>().text;
                        _submitScreen = true;
                    }
                    var bigLetters = ScreenTexts[2].text.Count(ch => ch == 'W' || ch == 'M');
                    ScreenTexts[2].fontSize = bigLetters > 5 ? 27 : bigLetters > 2 ? 30 : 35;
                }
                return false;
            };
        }

#pragma warning disable 414
        protected string TwitchHelpMessage = "!{0} right/left [move between screens] | !{0} submit answer";
#pragma warning restore 414

        protected IEnumerator ProcessTwitchCommand(string command)
        {
            if (command.EqualsIgnoreCase("right") || command.EqualsIgnoreCase("r"))
            {
                yield return null;
                yield return new[] { RightArrow };
                yield break;
            }

            if (command.EqualsIgnoreCase("left") || command.EqualsIgnoreCase("l"))
            {
                yield return null;
                yield return new[] { LeftArrow };
                yield break;
            }

            var split = command.ToUpperInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2 || !split[0].Equals("SUBMIT") || split[1].Length > 8)
                yield break;
            var buttons = split[1].Select(getPositionFromChar).ToArray();
            if (buttons.Any(x => x < 0))
                yield break;

            yield return null;

            foreach (var let in split[1])
            {
                yield return new WaitForSeconds(.1f);
                Keyboard[getPositionFromChar(let)].OnInteract();
            }
            yield return new WaitForSeconds(.25f);
            Submit.OnInteract();
            yield return new WaitForSeconds(.1f);
        }

        protected IEnumerator TwitchHandleForcedSolve()
        {
            if (_submitScreen && !_answer.StartsWith(ScreenTexts[2].text))
            {
                LeftArrow.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            for (var i = _submitScreen ? ScreenTexts[2].text.Length : 0; i < _answer.Length; i++)
            {
                Keyboard[getPositionFromChar(_answer[i])].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            Submit.OnInteract();
            yield return new WaitForSeconds(0.1f);
        }

        private int getPositionFromChar(char c)
        {
            return "QWERTYUIOPASDFGHJKLZXCVBNM".IndexOf(c);
        }

        void Update()
        {
            if (_moduleSelected)
            {
                for (var ltr = 0; ltr < 26; ltr++)
                    if (Input.GetKeyDown(((char) ('a' + ltr)).ToString()))
                        Keyboard[getPositionFromChar((char) ('A' + ltr))].OnInteract();
                if (Input.GetKeyDown(KeyCode.Return))
                    Submit.OnInteract();
            }
        }
    }
}