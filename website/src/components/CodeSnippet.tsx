import React from 'react';
import CodeBlock from '@theme/CodeBlock';

export type Props = {
  language?: string,
  title?: string,
  children: string,
}

export default function ({children, language, title}: Props): JSX.Element {
  return (
    <CodeBlock className={"language-" + (language || "csharp")} title={title}>
      {children.replace(/\t/g, '    ')}
    </CodeBlock>
  );
}