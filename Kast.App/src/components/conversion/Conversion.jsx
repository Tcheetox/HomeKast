import React, { useState } from 'react'

import './conversion.scoped.scss'
import Convert from '../../assets/icons/convert.svg'
import { useConversions } from '../../hooks'

export default function Conversion() {
  const { conversions } = useConversions()
  return (
    <div className='conversion'>
      <Convert />
    </div>
  )
}
