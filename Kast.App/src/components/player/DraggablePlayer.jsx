import React from 'react'

import { Drag, Conditional } from '../../hoc'
import Player from './Player'
import { useCasters, useSettings } from '../../hooks'

export default function DraggablePlayer() {
  const casters = useCasters()
  const settings = useSettings()
  const caster = casters.find(e => e.id === settings?.application?.receiverId)
  const show = caster?.isOwner || caster?.media
  if (show) console.log(caster)
  return (
    <Conditional test={show}>
      <Drag>
        <Player {...caster} />
      </Drag>
    </Conditional>
  )
}
