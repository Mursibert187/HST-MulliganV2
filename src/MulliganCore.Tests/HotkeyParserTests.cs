using HstMulligan.Core.Input;
using Xunit;

namespace HstMulligan.Core.Tests
{
    public class HotkeyParserTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Ctrl+")]
        [InlineData("++")]
        [InlineData("Ctrl+Shift+")]
        [InlineData("Ctrl+Fnope")]
        public void GarbageYieldsNone(string input)
        {
            var b = HotkeyParser.Parse(input);
            Assert.False(b.IsValid);
            Assert.Equal(HotkeyBinding.None, b);
        }

        [Fact]
        public void BareF9ParsesWithNoModifiers()
        {
            var b = HotkeyParser.Parse("F9");
            Assert.True(b.IsValid);
            Assert.Equal(HotkeyModifiers.None, b.Modifiers);
            Assert.Equal(0x78u, b.VirtualKey);
        }

        [Theory]
        [InlineData("F1", 0x70)]
        [InlineData("F12", 0x7B)]
        [InlineData("F24", 0x87)]
        public void FunctionKeysMapToWin32Range(string s, uint expected)
        {
            Assert.Equal(expected, HotkeyParser.Parse(s).VirtualKey);
        }

        [Theory]
        [InlineData("A",  (uint)'A')]
        [InlineData("z",  (uint)'Z')]
        [InlineData("0",  (uint)'0')]
        [InlineData("9",  (uint)'9')]
        public void LettersAndDigitsFoldToUpper(string s, uint expected)
        {
            Assert.Equal(expected, HotkeyParser.Parse(s).VirtualKey);
        }

        [Fact]
        public void ModifierStackingAccumulates()
        {
            var b = HotkeyParser.Parse("Ctrl+Shift+Alt+Win+M");
            Assert.Equal(
                HotkeyModifiers.Control | HotkeyModifiers.Shift | HotkeyModifiers.Alt | HotkeyModifiers.Win,
                b.Modifiers);
            Assert.Equal((uint)'M', b.VirtualKey);
        }

        [Theory]
        [InlineData("Control+F5")]
        [InlineData("ctrl+f5")]
        [InlineData("CTRL + F5")]
        [InlineData("  Ctrl +   F5  ")]
        public void SpacingAndCasingDoNotMatter(string s)
        {
            var b = HotkeyParser.Parse(s);
            Assert.Equal(HotkeyModifiers.Control, b.Modifiers);
            Assert.Equal(0x74u, b.VirtualKey);
        }

        [Theory]
        [InlineData("Escape",  0x1Bu)]
        [InlineData("Space",   0x20u)]
        [InlineData("Enter",   0x0Du)]
        [InlineData("Return",  0x0Du)]
        [InlineData("PageUp",  0x21u)]
        [InlineData("PGDN",    0x22u)]
        [InlineData("Left",    0x25u)]
        [InlineData("Down",    0x28u)]
        public void NamedKeysMap(string s, uint expected)
        {
            Assert.Equal(expected, HotkeyParser.Parse(s).VirtualKey);
        }

        [Fact]
        public void DuplicateModifiersAreIdempotent()
        {
            var b = HotkeyParser.Parse("Ctrl+Ctrl+F2");
            Assert.Equal(HotkeyModifiers.Control, b.Modifiers);
            Assert.Equal(0x71u, b.VirtualKey);
        }

        [Fact]
        public void WinAliasesAcceptMetaAndSuper()
        {
            Assert.Equal(HotkeyModifiers.Win, HotkeyParser.Parse("Meta+Space").Modifiers);
            Assert.Equal(HotkeyModifiers.Win, HotkeyParser.Parse("Super+Space").Modifiers);
            Assert.Equal(HotkeyModifiers.Win, HotkeyParser.Parse("Win+Space").Modifiers);
        }
    }
}
