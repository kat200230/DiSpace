/** @type {import('@docusaurus/types').DocusaurusConfig} */
module.exports = {
  title: "DiPeek",
  url: 'https://Abbysssal.github.com',
  baseUrl: '/DiSpace/',
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',
  favicon: 'img/favicon.ico',
  organizationName: 'Abbysssal',
  projectName: 'DiSpace',
  plugins: ['docusaurus-plugin-sass'],
  themeConfig: {
    hideableSidebar: true,
    prism: {
      theme: require('prism-react-renderer/themes/dracula'),
      additionalLanguages: ['clike', 'csharp', 'bash'],
    },
    announcementBar: {
      id: 'star',
      content:
        '<span style="font-size: 1rem;">⭐️ Если вам понравился DiPeek, поставьте ему звезду на <a target="_blank" href="https://github.com/Abbysssal/DiSpace">GitHub</a>! ⭐️</span>',
    },
    navbar: {
      hideOnScroll: true,
      title: "DiPeek",
      logo: {
        alt: '[[LOGO-ALT-TEXT]]',
        src: 'img/logo.png',
        srcDark: 'img/logo-dark.png',
      },
      items: [
        {
          to: 'docs/user/intro',
          label: 'Руководство пользователя',
          position: 'left',
        },
        {
          href: 'https://github.com/Abbysssal/DiSpace',
          position: 'right',
          className: 'header-github-link',
          'aria-label': 'GitHub repository',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Документация',
          items: [
            {
              label: 'Руководство пользователя',
              to: '/docs/user/intro',
            },
          ],
        },
        {
          title: 'Комьюнити',
          items: [
            {
              label: 'DiPeek Discord',
              href: 'https://discord.gg/tphsh9vsty',
            },
            {
              label: 'Сайт DiSpace',
              href: 'https://dispace.edu.nstu.ru/ditest/index',
            }
          ],
        },
        {
          title: 'Больше',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/Abbysssal/DiSpace',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} DiPeek. Built with Docusaurus.`,
    },
  },
  presets: [
    [
      '@docusaurus/preset-classic',
      {
        docs: {
          sidebarPath: require.resolve('./sidebars.js'),
          editUrl:
            'https://github.com/Abbysssal/DiSpace/edit/main/website/',
        },
        blog: {
          showReadingTime: true,
          editUrl:
            'https://github.com/Abbysssal/DiSpace/edit/main/website/blog/',
        },
        theme: {
          customCss: require.resolve('./src/css/custom.css'),
        },
      },
    ],
  ],
};
