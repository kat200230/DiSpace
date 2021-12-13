import React from 'react';
import clsx from 'clsx';
import Layout from '@theme/Layout';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import styles from './index.module.css';
import HomepageFeatures from '../components/HomepageFeatures';
import useThemeContext from '@theme/hooks/useThemeContext';
import Logo from '@site/static/img/logo.png';
import LogoDark from '@site/static/img/logo-dark.png';
import Translate from '@docusaurus/Translate';

function HomepageHeader() {
  const { isDarkTheme } = useThemeContext();
  return (
    <header className={clsx('hero hero--primary', styles.heroBanner)}>
      <div className="container">
        <img src={isDarkTheme ? LogoDark : Logo} width='20%'/>
        <p className="hero__subtitle">
          <Translate id="homepage.tagline">
            {"Крупнейшая база данных тестов DiSpace."}
          </Translate>
        </p>
        <div className={styles.buttons}>
          <Link
            className="button button--secondary button--lg"
            to="/docs/user/intro">
            <Translate id="homepage.button"
              description="The big button in the center on the home page">
              {"Руководство пользователя"}
            </Translate>
          </Link>
        </div>
      </div>
    </header>
  );
}

export default function Home() {
  const { siteConfig } = useDocusaurusContext();
  return (
    <Layout
      title={`${siteConfig.title}`}
      description="Крупнейшая база данных тестов DiSpace.">
      <HomepageHeader/>
      <main>
        <HomepageFeatures/>
      </main>
    </Layout>
  );
}
