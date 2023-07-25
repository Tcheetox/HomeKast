import React, { useRef, useEffect } from 'react'

// https://github.com/AndrewRedican/react-json-editor-ajrm#readme
// Hacking implementation to obtain kind of desired behavior

import JSONInput from 'react-json-editor-ajrm'
import locale from 'react-json-editor-ajrm/locale/en'

let cursor
let timer

export default function Advanced({ settings, setSettings }) {
  const ref = useRef()

  useEffect(() => {
    setTimeout(() => {
      if (ref.current && cursor) ref.current.setCursorPosition(cursor)
    })
  }, [settings])

  useEffect(() => {
    if (!ref.current) return
    ref.current.onKeyPress = () => {
      if (ref.current) cursor = ref.current.getCursorPosition()
      setTimeout(() => {
        const current = ref.current
        if (current) {
          clearTimeout(timer)
          const data = current.tokenize(current.refContent)?.jsObject
          if (data) timer = setTimeout(() => setSettings(data), 650)
        }
      })
    }
  }, [setSettings])

  return (
    <JSONInput
      ref={ref}
      className='advanced-settings'
      placeholder={settings}
      locale={locale}
      height='auto'
      width='auto'
      confirmGood={false}
      colors={{ background: '#191b1e' }}
      onKeyPressUpdate={false}
    />
  )
}
