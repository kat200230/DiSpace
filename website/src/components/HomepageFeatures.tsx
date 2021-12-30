import React from 'react';
import clsx from 'clsx';
import styles from './HomepageFeatures.module.css';
import Translate from '@docusaurus/Translate';
import emptyPng from '@site/static/img/empty.png';
import answersPng from '@site/static/img/answers.png';
import searchPng from '@site/static/img/search.png';
import interactivePng from '@site/static/img/interactive.png';
import storagePng from '@site/static/img/storage.png';

const FeatureList = [
  {
    title: (
      <Translate id="features.1">
        {"Крупнейшая база данных тестов"}
      </Translate>
    ),
    svg: emptyPng,
    description: (
      <Translate id="features.1.description">
        {"Нигде крупнее вы не найдёте. Кроме самих серверов DiSpace, естественно."}
      </Translate>
    ),
  },
  {
    title: (
      <Translate id="features.2">
        {"Просмотр ответов на тесты"}
      </Translate>
    ),
    svg: answersPng,
    description: (
      <Translate id="features.2.description">
        {"В базе данных хранятся ответы на все вопросы, которые когда-либо кому-то попадались."}
      </Translate>
    ),
  },
  {
    title: (
      <Translate id="features.3">
        {"Доступны все правильные ответы"}
      </Translate>
    ),
    svg: emptyPng,
    description: (
      <Translate id="features.3.description">
        {"Даже если никто никогда не отвечал на вопрос правильно, правильный ответ на вопрос всё равно будет известен."}
      </Translate>
    ),
  },
  {
    title: (
      <Translate id="features.4">
        {"Поиск тестов по названиям"}
      </Translate>
    ),
    svg: searchPng,
    description: (
      <Translate id="features.4.description">
        {"Тесты иногда подтираются или переайдировываются, поэтому поиск по названиям более надёжный."}
      </Translate>
    ),
  },
  {
    title: (
      <Translate id="features.5">
        {"Интерактивный режим ответов"}
      </Translate>
    ),
    svg: interactivePng,
    description: (
      <Translate id="features.5.description">
        {"Вы можете вводить айди отдельных ответов и получать подробную информацию по ним."}
      </Translate>
    ),
  },
  {
    title: (
      <Translate id="features.6">
        {"Надёжный сервер"}
      </Translate>
    ),
    svg: storagePng,
    description: (
      <Translate id="features.6.description">
        {"Даже если DiSpace удалит все тесты, база данных останется нетронутой."}
      </Translate>
    ),
  },
];

function Feature({svg, title, description}) {
  return (
    <div className={clsx('col col--4')}>
      <div className="text--center">
        <img src={svg} className={styles.featureSvg} alt={title}/>
      </div>
      <div className="text--center padding-horiz--md">
        <h3>{title}</h3>
        <p>{description}</p>
      </div>
    </div>
  );
}

export default function HomepageFeatures() {
  return (
    <section className={styles.features}>
      <div className="container">
        <div className="row">
          {FeatureList.map((props, idx) => (
            <Feature key={idx} {...props}/>
          ))}
        </div>
      </div>
    </section>
  );
}
