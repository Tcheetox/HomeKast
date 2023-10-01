import React, { useState } from 'react'

import { Row } from 'react-bootstrap'
import { Stop, Play, Pause, Next, Previous } from '../../assets/icons'
import { Case, DefaultCase, Switch } from '../../hoc'
import './player.scoped.scss'

export default function Player({ current, volume, title, duration }) {
  const [progress, setProgress] = useState(0)

  const prevent = e => e.preventDefault()

  return (
    <div className='player'>
      <div className='title'>{title ?? 'Unknown'}</div>
      <Row className='progress-bar'>
        <div className='tiny since'>{current ?? ''}</div>
        <input
          type='range'
          className='progress'
          value={progress}
          onChange={e => setProgress(e.target.value)}
          onDragStart={prevent}
          draggable={true}
        />
        <div className='tiny since'>{duration ?? ''}</div>
      </Row>
      <Row className='actions-bar'>
        <Previous />
        <Stop className='stop-icon' />
        <Switch test='media.playing'>
          <Case value='playing'>
            <Pause />
          </Case>
          <DefaultCase>
            <Pause className='pause-icon' />
            <Play className='play-icon' />
          </DefaultCase>
        </Switch>
        <Next />
      </Row>
      <input
        type='range'
        className='volume'
        min={0}
        value={volume * 100}
        max={100}
        // onChange={e => setProgress(e.target.value)}
        onDragStart={prevent}
        draggable={true}
      />
    </div>
  )
}
