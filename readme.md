# DIPS.Mobile.DesignTokens
A repository for maintaining and building Android, iOS and .NET MAUI style resources from exported Design Tokens from DIPS Mobile Design System. Design tokens makes it easier for developers and designers to communicate the style of the product by conventional names for the resources. The exported resources from this repository will be included in a shared UX components library for all mobile apps in DIPS to use.

## Maintaing design tokens
Design tokens must be placed in the `src` folder. The tokens are based on Amazon Style Dictionary [Design Tokens](https://amzn.github.io/style-dictionary/#/tokens?id=design-tokens).

## Building design styles
1. Make sure to [install Amazons Style Dictionary](https://github.com/amzn/style-dictionary#installation).
2. Clone / Fork this repository.
3. Run `./buildwindow.bash` in the repository root to build and transform the design tokens to resources
4. Open `output` folder and copy-pasta the resources to the platforms.

## Maintained by Team Mobil, DIPS AS.
