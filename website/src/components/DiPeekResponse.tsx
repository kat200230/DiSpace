import React from 'react';
import { DiscordMessage } from '@danktuary/react-discord-message';

export default function DiPeekResponse(props: any): JSX.Element {
  return (
    <DiscordMessage bot={true} author="DiPeek"
      roleColor="#FF0000"
      avatar="https://cdn.discordapp.com/avatars/914158586412802068/53ebdc456fa67eeec7cbff55cedb331d.png?size=256"
      {...props}/>
  );
}
