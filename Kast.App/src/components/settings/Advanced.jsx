import React, { useCallback } from 'react'

import './settings.scoped.scss'
import CodeMirror from '@uiw/react-codemirror'
import { javascript } from '@codemirror/lang-javascript'
import { githubDark } from '@uiw/codemirror-theme-github'

const extensions = [javascript()]

export default function Advanced({ settings, setSettings }) {
  const onChange = useCallback(
    value => {
      try {
        const jsonSettings = JSON.parse(value)
        setSettings(jsonSettings)
      } catch {
        // Not a valid JSON
      }
    },
    [setSettings]
  )

  return (
    <CodeMirror
      className='cm'
      theme={githubDark}
      value={JSON.stringify(settings, null, 4)}
      height='83vh'
      width='100%'
      extensions={extensions}
      onChange={onChange}
    />
  )
}
