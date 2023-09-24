import React from 'react'

import ListBuilder from './ListBuilder'
import DropPicker from './DropPicker'
import { useCasters } from '../../hooks'

export default function Basic({ settings, setSettings }) {
  const casters = useCasters()
  const preferredCaster = casters.find(c => c.id === settings.application?.receiverId)

  return (
    <div className='basic-settings'>
      <DropPicker
        name='Chromecast'
        value={preferredCaster?.name}
        values={casters.map(c => c.name)}
        setValue={v => setSettings(s => ({ ...s, application: { ...s.application, receiverId: casters.find(c => c.name === v).id } }))}
      />
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
