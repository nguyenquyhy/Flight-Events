﻿import * as React from 'react';
import styled from 'styled-components';
import { IMap } from '../maps/IMap';

interface Props {
    map: IMap;
}

const Ruler = (props: Props) => {
    const handleClick = () => {
        props.map.startDrawing();
    }

    return <CollapsedDiv onClick={handleClick} title="Measuring distance and bearing on the map">
        <svg xmlns="http://www.w3.org/2000/svg" height="40" width="40" viewBox="0 0 144 144">
            <g>
                <path id="path1" transform="rotate(0,72,72) translate(32,38.875) scale(2.5,2.5)" fill="#000000" d="M29.971972,4.8259844L24.784748,10.018165 27.813252,13.188042C28.004265,13.388025 27.997265,13.705 27.797251,13.894985 27.701243,13.987978 27.576235,14.033974 27.452225,14.033974 27.320216,14.033974 27.189207,13.981978 27.0912,13.879987L24.077721,10.72587 22.740904,12.063964 24.838141,14.248068C25.029152,14.448049 25.022152,14.765019 24.823139,14.955001 24.727133,15.048992 24.602125,15.094988 24.477118,15.094988 24.34611,15.094988 24.2141,15.042993 24.116095,14.942002L22.033303,12.772242 20.692374,14.114452 23.841227,17.411047C24.032246,17.61103 24.025246,17.928005 23.825226,18.11799 23.729218,18.210983 23.604204,18.256979 23.480192,18.256979 23.348181,18.256979 23.217167,18.204983 23.119158,18.102991L19.985335,14.822168 18.648146,16.160635 20.867131,18.47098C21.058144,18.670977 21.051144,18.986975 20.852131,19.177973 20.756125,19.271971 20.631117,19.31797 20.50611,19.31797 20.375101,19.31797 20.243092,19.265972 20.145087,19.164972L17.940537,16.868919 16.599678,18.211059 19.870219,21.635052C20.061238,21.835037 20.054238,22.152012 19.854218,22.341995 19.758208,22.434988 19.633196,22.480986 19.509184,22.480986 19.377171,22.480986 19.246159,22.428989 19.14815,22.326998L15.892637,18.918777 14.554687,20.258005 16.89514,22.694084C17.086129,22.894064 17.079129,23.210031 16.880141,23.401012 16.784146,23.495005 16.659153,23.540998 16.534161,23.540998 16.40317,23.540998 16.272177,23.489004 16.173184,23.388014L13.847575,20.965794 12.507484,22.307165 15.89921,25.85807C16.090229,26.058058 16.083227,26.375036 15.883209,26.565023 15.787199,26.658016 15.662188,26.704014 15.538176,26.704014 15.406163,26.704014 15.275151,26.652018 15.177141,26.550024L11.800437,23.014887 10.462217,24.354387 12.924132,26.917997C13.11512,27.117998 13.10812,27.435001 12.909132,27.625004 12.813138,27.719005 12.688146,27.765005 12.563153,27.765005 12.432161,27.765005 12.300168,27.713005 12.202174,27.612003L9.7551034,25.062176 4.8280008,29.993988 29.999987,29.980987z M31.966967,0L32.002001,31.978971 0,31.996z" />
            </g>
        </svg>
    </CollapsedDiv>
}

const CollapsedDiv = styled.div`
pointer-events: auto;
position: absolute;
top: 128px;
left: 10px;
z-index: 1000;
width: 40px;
height: 40px;
box-shadow: 0 1px 5px rgba(0,0,0,0.65);
border-radius: 4px;
background-color: rgba(255,255,255,1);
opacity: 0.8;
cursor: pointer;
`

export default Ruler;