import React from 'react'

import ListBuilder from './ListBuilder'

export default function Basic({ settings, setSettings }) {
  return (
    <div className='basic-settings'>
      <ListBuilder
        name='Extensions'
        list={settings.library?.extensions ?? []}
        placeholder='.ext'
        setList={v => setSettings(s => ({ ...s, library: { ...s.library, extensions: v } }))}
        transform={e => (e.startsWith('.') ? e : `.${e}`)}
      />
      <ListBuilder
        name='Directories'
        list={settings.library?.directories ?? []}
        placeholder='Folder path'
        setList={v => setSettings(s => ({ ...s, library: { ...s.library, directories: v } }))}
      />
    </div>
  )
}
