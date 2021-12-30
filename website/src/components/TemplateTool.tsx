import React, { useState } from 'react';
import styles from './TemplateTool.module.css';
import clsx from 'clsx';

export type Props = {
  defaultTemplate?: string,
  defaultText?: string,
}

export default function TemplateChecker({defaultTemplate, defaultText, ...props}: Props) {
  const [template, setTemplate] = useState(defaultTemplate || "");
  const [text, setText] = useState('однородный' || "");

  let regexSrc = template.replace(/\*/g, ".*");
  let insensitive = regexSrc.toLowerCase() == regexSrc;
  let regex = new RegExp('^' + regexSrc + '$', insensitive ? 'i' : undefined);
  let check = regex.test(text);

  return (
    <div className={styles.container}>
      <div>
        <input className={styles.template} defaultValue={template}
          type="text" placeholder="шаблон ответа" onChange={e => setTemplate(e.target.value)}/>
        <abbr title={insensitive ? "не чувствителен к регистру" : "чувствителен к регистру"}
          className={insensitive ? styles.insensitive : styles.sensitive}>
          {"Aa"}
        </abbr>
      </div>
      <input className={clsx(styles.text, check && styles.match)} defaultValue={text}
        type="text" placeholder="проверка ответа" onChange={e => setText(e.target.value)}/>
    </div>
  );
}
