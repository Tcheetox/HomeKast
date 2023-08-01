import React from 'react'

import './conversion.scoped.scss'
import Convert from '../../assets/icons/convert.svg'
import ConversionPopover from './ConversionPopover'
import { useConversions } from '../../hooks'
import { Conditional } from '../../hoc'
import { OverlayTrigger, Button, Popover } from 'react-bootstrap'

export default function Conversion() {
  const { conversions, stopConversion, isConverting } = useConversions()

  return (
    <div className={`conversion ${isConverting ? 'visible' : 'hidden'}`}>
      <Conditional test={isConverting}>
        <OverlayTrigger
          trigger='click'
          key={'conversion-popover'}
          placement={'bottom'}
          overlay={
            <Popover>
              <ConversionPopover conversions={conversions} />
            </Popover>
          }
        >
          <Button className='btn-unstyle' onClick={stopConversion}>
            <Convert />
          </Button>
        </OverlayTrigger>
      </Conditional>
    </div>
  )
}
